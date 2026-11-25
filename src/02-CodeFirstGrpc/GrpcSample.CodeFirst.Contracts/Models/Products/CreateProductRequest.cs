using System.Runtime.Serialization;
using GrpcSample.Domain.Models.Products;

namespace GrpcSample.CodeFirst.Contracts.Models.Products;

[DataContract]
public sealed class CreateProductRequest
{
    [DataMember(Order = 1)] public string Sku { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Name { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Description { get; set; } = string.Empty;
    [DataMember(Order = 4)] public ProductCategory Category { get; set; }
    [DataMember(Order = 5)] public decimal Price { get; set; }
    [DataMember(Order = 6)] public int StockQuantity { get; set; }
    [DataMember(Order = 7)] public double Rating { get; set; } = 4.5;
    [DataMember(Order = 8)] public List<string> Tags { get; set; } = [];
    [DataMember(Order = 9)] public Dictionary<string, string> Attributes { get; set; } = [];
}
