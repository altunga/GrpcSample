using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface IProductRepository
{
    Product GetById(string id);
    Product? FindBySku(string sku);
    PagedResult<Product> List(ProductFilter filter);
    IReadOnlyList<Product> Stream(ProductFilter filter);
    Product Create(CreateProductInput input, DateTime createdAtUtc);
    Product Update(UpdateProductInput input);
    bool Delete(string id);
    IReadOnlyList<OrderLineItem> ReserveStockAtomically(IReadOnlyList<CreateOrderItemInput> items);
    IReadOnlyList<Product> GetAllSnapshot();
}
