using System.Runtime.Serialization;
using GrpcSample.Domain.Models.Orders;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class OrderModel
{
    [DataMember(Order = 1)] public string Id { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string CustomerId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public OrderStatus Status { get; set; }
    [DataMember(Order = 4)] public List<OrderLineItemModel> Items { get; set; } = [];
    [DataMember(Order = 5)] public decimal TotalAmount { get; set; }
    [DataMember(Order = 6)] public string ShippingAddress { get; set; } = string.Empty;
    [DataMember(Order = 7)] public string? TrackingNumber { get; set; }
    [DataMember(Order = 8)] public DateTime CreatedAtUtc { get; set; }
}
