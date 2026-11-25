using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Products;

[MessagePackObject]
public sealed class ListProductsResponse
{
    [Key(0)] public List<ProductModel> Items { get; set; } = [];
    [Key(1)] public int TotalCount { get; set; }
    [Key(2)] public int Page { get; set; }
    [Key(3)] public int PageSize { get; set; }
}
