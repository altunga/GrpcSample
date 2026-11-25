using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Orders;

[MessagePackObject]
public sealed class OrderLineItemModel
{
    [Key(0)] public string ProductId { get; set; } = string.Empty;
    [Key(1)] public string Sku { get; set; } = string.Empty;
    [Key(2)] public string ProductName { get; set; } = string.Empty;
    [Key(3)] public int Quantity { get; set; }
    [Key(4)] public decimal UnitPrice { get; set; }
}
