using System.Runtime.Serialization;
using GrpcSample.Domain.Models.Products;

namespace GrpcSample.CodeFirst.Contracts.Models.Products;

[DataContract]
public sealed class ProductModel
{
    [DataMember(Order = 1)] public string Id { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Sku { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Name { get; set; } = string.Empty;
    [DataMember(Order = 4)] public string Description { get; set; } = string.Empty;
    [DataMember(Order = 5)] public ProductCategory Category { get; set; }
    [DataMember(Order = 6)] public decimal Price { get; set; }
    [DataMember(Order = 7)] public int StockQuantity { get; set; }
    [DataMember(Order = 8)] public double Rating { get; set; }
    [DataMember(Order = 9)] public List<string> Tags { get; set; } = [];
    [DataMember(Order = 10)] public Dictionary<string, string> Attributes { get; set; } = [];
    [DataMember(Order = 11)] public bool IsActive { get; set; }
    [DataMember(Order = 12)] public DateTime CreatedAtUtc { get; set; }
}
