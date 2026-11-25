using System.Runtime.Serialization;
using GrpcSample.Domain.Models.Customers;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class PriceQuoteRequest
{
    [DataMember(Order = 1)] public string Sku { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Quantity { get; set; }
    [DataMember(Order = 3)] public CustomerTier CustomerTier { get; set; }
}
