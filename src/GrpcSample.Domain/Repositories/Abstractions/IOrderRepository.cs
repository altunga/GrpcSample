using GrpcSample.Domain.Models.Orders;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface IOrderRepository
{
    Order GetById(string id);
    IReadOnlyList<Order> ListByCustomer(string customerId);
    Order Add(
        string customerId,
        string shippingAddress,
        IReadOnlyList<OrderLineItem> items,
        decimal totalAmount,
        DateTime createdAtUtc);
    IReadOnlyList<Order> GetAllSnapshot();
}
