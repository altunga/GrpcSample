using GrpcSample.Domain.Models.Products;

namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record UpdateProductInput(
    string Id,
    string Name,
    string Description,
    ProductCategory Category,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    List<string>? Tags,
    Dictionary<string, string>? Attributes);
