using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Pricing;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Services.Abstractions;

public interface IOrderProcessingService
{
    Order GetOrder(string id);
    IReadOnlyList<Order> ListOrdersByCustomer(string customerId);
    Order CreateOrder(CreateOrderInput input);
    QuoteResult CalculateQuote(string sku, int quantity, CustomerTier tier, decimal currentSubtotal);
}
