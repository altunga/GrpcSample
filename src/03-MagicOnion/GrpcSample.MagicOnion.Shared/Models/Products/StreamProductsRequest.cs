using GrpcSample.Domain.Models.Products;
using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Products;

[MessagePackObject]
public sealed class StreamProductsRequest
{
    [Key(0)] public ProductCategory? Category { get; set; }
    [Key(1)] public bool ActiveOnly { get; set; }
    [Key(2)] public int DelayMs { get; set; }
}
