using GrpcSample.Domain.Models.Products;

namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record CreateProductInput(
    string Sku,
    string Name,
    string Description,
    ProductCategory Category,
    decimal Price,
    int StockQuantity,
    double Rating,
    List<string>? Tags,
    Dictionary<string, string>? Attributes);
