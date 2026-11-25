using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Products;

[DataContract]
public sealed class ListProductsResponse
{
    [DataMember(Order = 1)] public List<ProductModel> Items { get; set; } = [];
    [DataMember(Order = 2)] public int TotalCount { get; set; }
    [DataMember(Order = 3)] public int Page { get; set; }
    [DataMember(Order = 4)] public int PageSize { get; set; }
}
