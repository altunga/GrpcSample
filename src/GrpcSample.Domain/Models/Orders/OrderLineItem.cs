namespace GrpcSample.Domain.Models.Orders;

public sealed class OrderLineItem
{
    public required string ProductId { get; set; }
    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
