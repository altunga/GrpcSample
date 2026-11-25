using GrpcSample.Domain.Models.Products;
using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Products;

[MessagePackObject]
public sealed class UpdateProductRequest
{
    [Key(0)] public string Id { get; set; } = string.Empty;
    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public string Description { get; set; } = string.Empty;
    [Key(3)] public ProductCategory Category { get; set; }
    [Key(4)] public decimal Price { get; set; }
    [Key(5)] public int StockQuantity { get; set; }
    [Key(6)] public bool IsActive { get; set; } = true;
    [Key(7)] public List<string> Tags { get; set; } = [];
    [Key(8)] public Dictionary<string, string> Attributes { get; set; } = [];
}
