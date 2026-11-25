using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Orders;

[MessagePackObject]
public sealed class CreateOrderRequest
{
    [Key(0)] public string CustomerId { get; set; } = string.Empty;
    [Key(1)] public string ShippingAddress { get; set; } = string.Empty;
    [Key(2)] public List<CreateOrderLineRequest> Items { get; set; } = [];
}
