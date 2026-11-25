using GrpcSample.Domain.Models.Catalog;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Services.Abstractions;

public interface IProductCatalogService
{
    Product GetProduct(string id);
    PagedResult<Product> ListProducts(ProductFilter filter);
    IReadOnlyList<Product> StreamProducts(ProductFilter filter);
    Product CreateProduct(CreateProductInput input);
    Product UpdateProduct(UpdateProductInput input);
    bool DeleteProduct(string id);
    CatalogSummary GetCatalogSummary();
}
