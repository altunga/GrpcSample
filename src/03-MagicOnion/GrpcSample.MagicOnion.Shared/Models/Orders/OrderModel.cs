using GrpcSample.Domain.Models.Orders;
using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Orders;

[MessagePackObject]
public sealed class OrderModel
{
    [Key(0)] public string Id { get; set; } = string.Empty;
    [Key(1)] public string CustomerId { get; set; } = string.Empty;
    [Key(2)] public OrderStatus Status { get; set; }
    [Key(3)] public List<OrderLineItemModel> Items { get; set; } = [];
    [Key(4)] public decimal TotalAmount { get; set; }
    [Key(5)] public string ShippingAddress { get; set; } = string.Empty;
    [Key(6)] public string? TrackingNumber { get; set; }
    [Key(7)] public DateTime CreatedAtUtc { get; set; }
}
