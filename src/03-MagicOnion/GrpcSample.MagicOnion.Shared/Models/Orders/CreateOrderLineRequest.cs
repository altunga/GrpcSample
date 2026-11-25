using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Orders;

[MessagePackObject]
public sealed class CreateOrderLineRequest
{
    [Key(0)] public string ProductId { get; set; } = string.Empty;
    [Key(1)] public int Quantity { get; set; }
}
