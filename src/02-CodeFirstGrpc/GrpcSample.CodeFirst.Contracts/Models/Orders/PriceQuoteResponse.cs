using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class PriceQuoteResponse
{
    [DataMember(Order = 1)] public string Sku { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ProductName { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int RequestedQuantity { get; set; }
    [DataMember(Order = 4)] public decimal UnitPrice { get; set; }
    [DataMember(Order = 5)] public decimal DiscountPercent { get; set; }
    [DataMember(Order = 6)] public decimal LineTotal { get; set; }
    [DataMember(Order = 7)] public decimal RunningSubtotal { get; set; }
    [DataMember(Order = 8)] public bool InStock { get; set; }
    [DataMember(Order = 9)] public string Note { get; set; } = string.Empty;
}
