using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class OrderLineItemModel
{
    [DataMember(Order = 1)] public string ProductId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Sku { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string ProductName { get; set; } = string.Empty;
    [DataMember(Order = 4)] public int Quantity { get; set; }
    [DataMember(Order = 5)] public decimal UnitPrice { get; set; }
}
