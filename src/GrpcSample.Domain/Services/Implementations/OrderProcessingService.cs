using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Orders;
using GrpcSample.Domain.Models.Pricing;
using GrpcSample.Domain.Policies.Pricing;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services.Abstractions;

namespace GrpcSample.Domain.Services.Implementations;

public sealed class OrderProcessingService(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IPricingStrategy pricingStrategy,
    TimeProvider timeProvider) : IOrderProcessingService
{
    public Order GetOrder(string id) => orderRepository.GetById(id);

    public IReadOnlyList<Order> ListOrdersByCustomer(string customerId)
    {
        _ = customerRepository.GetById(customerId);
        return orderRepository.ListByCustomer(customerId);
    }

    public Order CreateOrder(CreateOrderInput input)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerId))
            throw new DomainValidationException("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(input.ShippingAddress))
            throw new DomainValidationException("ShippingAddress is required.");
        if (input.Items is null || input.Items.Count == 0)
            throw new DomainValidationException("Order must contain at least one line item.");

        var customer = customerRepository.GetById(input.CustomerId);
        var reservedItems = productRepository.ReserveStockAtomically(input.Items);
        var totalAmount = reservedItems.Sum(i => i.UnitPrice * i.Quantity);

        var order = orderRepository.Add(
            customer.Id,
            input.ShippingAddress,
            reservedItems,
            totalAmount,
            timeProvider.GetUtcNow().UtcDateTime);

        customerRepository.RecordSpending(customer.Id, order.TotalAmount);
        return order;
    }

    public QuoteResult CalculateQuote(string sku, int quantity, CustomerTier tier, decimal currentSubtotal)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainValidationException("SKU is required for price quote.");
        if (quantity <= 0)
            throw new DomainValidationException("Quote quantity must be positive.");

        var product = productRepository.FindBySku(sku)
            ?? throw new DomainNotFoundException($"Product with SKU '{sku}' was not found.");

        return pricingStrategy.CalculateQuote(product, quantity, tier, currentSubtotal);
    }
}
