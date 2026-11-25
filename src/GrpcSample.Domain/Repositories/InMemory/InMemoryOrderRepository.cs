using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryOrderRepository(InMemoryCommerceStore store) : IOrderRepository
{
    public Order GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainValidationException("Order ID is required.");

        if (store.Orders.TryGetValue(id.Trim(), out var order))
            return order.Clone();

        throw new DomainNotFoundException($"Order '{id}' was not found.");
    }

    public IReadOnlyList<Order> ListByCustomer(string customerId) =>
        store.Orders.Values
            .Where(o => string.Equals(o.CustomerId, customerId.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => o.Clone())
            .ToList();

    public Order Add(
        string customerId,
        string shippingAddress,
        IReadOnlyList<OrderLineItem> items,
        decimal totalAmount,
        DateTime createdAtUtc)
    {
        var orderId = store.NextOrderId();
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Items = [.. items],
            TotalAmount = Math.Round(totalAmount, 2),
            ShippingAddress = shippingAddress.Trim(),
            TrackingNumber = null,
            CreatedAtUtc = createdAtUtc
        };

        store.Orders[orderId] = order;
        return order.Clone();
    }

    public IReadOnlyList<Order> GetAllSnapshot() =>
        store.Orders.Values.Select(o => o.Clone()).ToList();
}
