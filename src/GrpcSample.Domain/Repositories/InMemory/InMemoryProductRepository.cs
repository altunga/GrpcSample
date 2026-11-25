using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryProductRepository(InMemoryCommerceStore store) : IProductRepository
{
    public Product GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainValidationException("Product ID is required.");

        if (store.Products.TryGetValue(id.Trim(), out var product))
            return product.Clone();

        throw new DomainNotFoundException($"Product '{id}' was not found.");
    }

    public Product? FindBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        return store.Products.Values
            .FirstOrDefault(p => string.Equals(p.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase))
            ?.Clone();
    }

    public PagedResult<Product> List(ProductFilter filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        var filtered = ApplyFilter(filter).ToList();
        var paged = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.Clone())
            .ToList();

        return new PagedResult<Product>(paged, filtered.Count, page, pageSize);
    }

    public IReadOnlyList<Product> Stream(ProductFilter filter) =>
        ApplyFilter(filter).Select(p => p.Clone()).ToList();

    public Product Create(CreateProductInput input, DateTime createdAtUtc)
    {
        lock (store.ProductLock)
        {
            var normalizedSku = input.Sku.Trim();
            if (store.Products.Values.Any(p => string.Equals(p.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase)))
                throw new DomainAlreadyExistsException($"Product with SKU '{normalizedSku}' already exists.");

            var id = store.NextProductId();
            var product = new Product
            {
                Id = id,
                Sku = normalizedSku,
                Name = input.Name.Trim(),
                Description = input.Description?.Trim() ?? string.Empty,
                Category = input.Category,
                Price = Math.Round(input.Price, 2),
                StockQuantity = input.StockQuantity,
                Rating = input.Rating <= 0 ? 4.5 : Math.Clamp(input.Rating, 1.0, 5.0),
                Tags = input.Tags is { Count: > 0 } ? [.. input.Tags] : [input.Category.ToString().ToLowerInvariant()],
                Attributes = input.Attributes is not null
                    ? new Dictionary<string, string>(input.Attributes, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                IsActive = true,
                CreatedAtUtc = createdAtUtc
            };

            store.Products[id] = product;
            return product.Clone();
        }
    }

    public Product Update(UpdateProductInput input)
    {
        lock (store.ProductLock)
        {
            if (!store.Products.TryGetValue(input.Id.Trim(), out var existing))
                throw new DomainNotFoundException($"Product '{input.Id}' was not found.");

            existing.Name = input.Name.Trim();
            existing.Description = input.Description?.Trim() ?? string.Empty;
            existing.Category = input.Category;
            existing.Price = Math.Round(input.Price, 2);
            existing.StockQuantity = input.StockQuantity;
            existing.IsActive = input.IsActive;
            if (input.Tags is not null)
                existing.Tags = [.. input.Tags];
            if (input.Attributes is not null)
                existing.Attributes = new Dictionary<string, string>(input.Attributes, StringComparer.OrdinalIgnoreCase);

            return existing.Clone();
        }
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainValidationException("Product ID is required.");

        if (!store.Products.TryRemove(id.Trim(), out _))
            throw new DomainNotFoundException($"Product '{id}' was not found.");

        return true;
    }

    public IReadOnlyList<OrderLineItem> ReserveStockAtomically(IReadOnlyList<CreateOrderItemInput> items)
    {
        lock (store.ProductLock)
        {
            var resolved = new List<OrderLineItem>(items.Count);
            var toDeduct = new List<(Product Product, int Qty)>(items.Count);

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    throw new DomainValidationException($"Quantity for product '{item.ProductId}' must be positive.");

                if (!store.Products.TryGetValue(item.ProductId.Trim(), out var product))
                    throw new DomainNotFoundException($"Product '{item.ProductId}' was not found.");

                if (!product.IsActive)
                    throw new DomainPreconditionFailedException($"Product '{product.Id}' is inactive and cannot be ordered.");

                if (product.StockQuantity < item.Quantity)
                {
                    throw new DomainPreconditionFailedException(
                        $"Insufficient stock for '{product.Id}' ({product.Name}). Requested={item.Quantity}, Available={product.StockQuantity}.");
                }

                toDeduct.Add((product, item.Quantity));
                resolved.Add(new OrderLineItem
                {
                    ProductId = product.Id,
                    Sku = product.Sku,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            // Deduct only after all items pass validation so failure never corrupts stock.
            foreach (var (product, qty) in toDeduct)
                product.StockQuantity -= qty;

            return resolved;
        }
    }

    public IReadOnlyList<Product> GetAllSnapshot() =>
        store.Products.Values.Select(p => p.Clone()).ToList();

    private IEnumerable<Product> ApplyFilter(ProductFilter filter)
    {
        IEnumerable<Product> query = store.Products.Values;

        if (filter.Category.HasValue)
            query = query.Where(p => p.Category == filter.Category.Value);

        if (filter.ActiveOnly)
            query = query.Where(p => p.IsActive);

        if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        return query.OrderBy(p => p.Id, StringComparer.Ordinal);
    }
}
