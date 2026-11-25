using GrpcSample.Domain.Models.Products;
using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Products;

[MessagePackObject]
public sealed class ProductModel
{
    [Key(0)] public string Id { get; set; } = string.Empty;
    [Key(1)] public string Sku { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public string Description { get; set; } = string.Empty;
    [Key(4)] public ProductCategory Category { get; set; }
    [Key(5)] public decimal Price { get; set; }
    [Key(6)] public int StockQuantity { get; set; }
    [Key(7)] public double Rating { get; set; }
    [Key(8)] public List<string> Tags { get; set; } = [];
    [Key(9)] public Dictionary<string, string> Attributes { get; set; } = [];
    [Key(10)] public bool IsActive { get; set; }
    [Key(11)] public DateTime CreatedAtUtc { get; set; }
}
