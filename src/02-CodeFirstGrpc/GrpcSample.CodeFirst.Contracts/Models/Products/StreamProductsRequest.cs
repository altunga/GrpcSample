using System.Runtime.Serialization;
using GrpcSample.Domain.Models.Products;

namespace GrpcSample.CodeFirst.Contracts.Models.Products;

[DataContract]
public sealed class StreamProductsRequest
{
    [DataMember(Order = 1)] public ProductCategory? Category { get; set; }
    [DataMember(Order = 2)] public bool ActiveOnly { get; set; }
    [DataMember(Order = 3)] public int DelayMs { get; set; }
}
