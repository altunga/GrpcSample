using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Orders;

[MessagePackObject]
public sealed class PriceQuoteResponse
{
    [Key(0)] public string Sku { get; set; } = string.Empty;
    [Key(1)] public string ProductName { get; set; } = string.Empty;
    [Key(2)] public int RequestedQuantity { get; set; }
    [Key(3)] public decimal UnitPrice { get; set; }
    [Key(4)] public decimal DiscountPercent { get; set; }
    [Key(5)] public decimal LineTotal { get; set; }
    [Key(6)] public decimal RunningSubtotal { get; set; }
    [Key(7)] public bool InStock { get; set; }
    [Key(8)] public string Note { get; set; } = string.Empty;
}
