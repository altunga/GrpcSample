using System.Runtime.Serialization;
using GrpcSample.Domain.Models.Products;

namespace GrpcSample.CodeFirst.Contracts.Models.Products;

[DataContract]
public sealed class ListProductsRequest
{
    [DataMember(Order = 1)] public ProductCategory? Category { get; set; }
    [DataMember(Order = 2)] public string? SearchTerm { get; set; }
    [DataMember(Order = 3)] public bool ActiveOnly { get; set; }
    [DataMember(Order = 4)] public decimal? MaxPrice { get; set; }
    [DataMember(Order = 5)] public int Page { get; set; } = 1;
    [DataMember(Order = 6)] public int PageSize { get; set; } = 20;
}
