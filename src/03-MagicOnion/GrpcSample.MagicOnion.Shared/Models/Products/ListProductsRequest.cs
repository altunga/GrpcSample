using GrpcSample.Domain.Models.Products;
using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Products;

[MessagePackObject]
public sealed class ListProductsRequest
{
    [Key(0)] public ProductCategory? Category { get; set; }
    [Key(1)] public string? SearchTerm { get; set; }
    [Key(2)] public bool ActiveOnly { get; set; }
    [Key(3)] public decimal? MaxPrice { get; set; }
    [Key(4)] public int Page { get; set; } = 1;
    [Key(5)] public int PageSize { get; set; } = 20;
}
