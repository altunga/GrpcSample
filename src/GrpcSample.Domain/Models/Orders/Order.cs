namespace GrpcSample.Domain.Models.Orders;

public sealed class Order
{
    public required string Id { get; set; }
    public required string CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderLineItem> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public required string ShippingAddress { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Order Clone() => new()
    {
        Id = Id,
        CustomerId = CustomerId,
        Status = Status,
        Items = Items.Select(i => new OrderLineItem
        {
            ProductId = i.ProductId,
            Sku = i.Sku,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        TotalAmount = TotalAmount,
        ShippingAddress = ShippingAddress,
        TrackingNumber = TrackingNumber,
        CreatedAtUtc = CreatedAtUtc
    };
}
