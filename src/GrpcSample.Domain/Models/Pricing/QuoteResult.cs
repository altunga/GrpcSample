namespace GrpcSample.Domain.Models.Pricing;

public sealed class QuoteResult
{
    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public int RequestedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }
    public decimal RunningSubtotal { get; set; }
    public bool InStock { get; set; }
    public required string Note { get; set; }
}
