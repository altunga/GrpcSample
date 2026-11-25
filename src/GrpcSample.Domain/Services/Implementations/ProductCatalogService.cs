using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models.Catalog;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services.Abstractions;

namespace GrpcSample.Domain.Services.Implementations;

public sealed class ProductCatalogService(
    IProductRepository productRepository,
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    ITelemetryRepository telemetryRepository,
    IBlockRepository blockRepository,
    TimeProvider timeProvider) : IProductCatalogService
{
    public Product GetProduct(string id) => productRepository.GetById(id);

    public PagedResult<Product> ListProducts(ProductFilter filter) => productRepository.List(filter);

    public IReadOnlyList<Product> StreamProducts(ProductFilter filter) => productRepository.Stream(filter);

    public Product CreateProduct(CreateProductInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sku))
            throw new DomainValidationException("Product SKU is required.");
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new DomainValidationException("Product Name is required.");
        if (input.Price <= 0)
            throw new DomainValidationException("Product Price must be greater than zero.");
        if (input.StockQuantity < 0)
            throw new DomainValidationException("StockQuantity cannot be negative.");

        return productRepository.Create(input, timeProvider.GetUtcNow().UtcDateTime);
    }

    public Product UpdateProduct(UpdateProductInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Id))
            throw new DomainValidationException("Product ID is required.");
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new DomainValidationException("Product Name is required.");
        if (input.Price <= 0)
            throw new DomainValidationException("Product Price must be greater than zero.");
        if (input.StockQuantity < 0)
            throw new DomainValidationException("StockQuantity cannot be negative.");

        return productRepository.Update(input);
    }

    public bool DeleteProduct(string id) => productRepository.Delete(id);

    public CatalogSummary GetCatalogSummary()
    {
        var products = productRepository.GetAllSnapshot();
        var orders = orderRepository.GetAllSnapshot();

        return new CatalogSummary
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive),
            TotalStockUnits = products.Sum(p => p.StockQuantity),
            TotalInventoryValue = Math.Round(products.Sum(p => p.Price * p.StockQuantity), 2),
            TotalCustomers = customerRepository.Count(),
            TotalOrders = orders.Count,
            TotalRevenue = Math.Round(orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount), 2),
            TotalTelemetryReadings = telemetryRepository.Count(),
            TotalBlocks = blockRepository.Count(),
            ProductsByCategory = products
                .GroupBy(p => p.Category.ToString())
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase)
        };
    }
}
