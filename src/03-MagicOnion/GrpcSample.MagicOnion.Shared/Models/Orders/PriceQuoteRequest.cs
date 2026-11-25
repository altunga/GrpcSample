using GrpcSample.Domain.Models.Customers;
using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Orders;

[MessagePackObject]
public sealed class PriceQuoteRequest
{
    [Key(0)] public string Sku { get; set; } = string.Empty;
    [Key(1)] public int Quantity { get; set; }
    [Key(2)] public CustomerTier CustomerTier { get; set; }
}
