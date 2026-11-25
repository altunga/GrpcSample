using GrpcSample.Domain.Configuration;
using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Policies.Pricing;
using GrpcSample.Domain.Policies.Pricing.Rules;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Repositories.InMemory;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Implementations;
using GrpcSample.Domain.Tests.TestDoubles;
using Xunit;

namespace GrpcSample.Domain.Tests;

public sealed class OrderProcessingAndPricingTests
{
    private static (OrderProcessingService Service, InMemoryProductRepository Products) CreateSut(
        IPricingStrategy? pricingStrategy = null,
        TimeProvider? timeProvider = null)
    {
        var store = InMemoryCommerceStore.CreateSeeded();
        var products = new InMemoryProductRepository(store);
        var strategy = pricingStrategy ?? new TieredVolumePricingStrategy(
            [new CustomerTierDiscountRule(), new VolumeDiscountRule()],
            new PricingOptions());

        var service = new OrderProcessingService(
            new InMemoryOrderRepository(store),
            new InMemoryCustomerRepository(store),
            products,
            strategy,
            timeProvider ?? TimeProvider.System);

        return (service, products);
    }

    [Fact]
    public void CreateOrder_DeductsStockAtomically_AndRejectsPartialOrInactiveFailures()
    {
        var fixedNow = new DateTimeOffset(2026, 7, 1, 8, 30, 0, TimeSpan.Zero);
        var (service, products) = CreateSut(timeProvider: new FixedTimeProvider(fixedNow));
        var before1 = products.GetById("PRD-001");
        var before2 = products.GetById("PRD-002");

        var order = service.CreateOrder(new CreateOrderInput(
            CustomerId: "CUS-001",
            ShippingAddress: "123 Cloud St, Istanbul",
            Items: [new CreateOrderItemInput("PRD-001", 2)]));

        Assert.Equal(fixedNow.UtcDateTime, order.CreatedAtUtc);
        Assert.Equal(before1.StockQuantity - 2, products.GetById("PRD-001").StockQuantity);
        Assert.Equal(before1.Price * 2, order.TotalAmount);

        // Multi-item order where second item fails stock check must NOT deduct first item!
        Assert.Throws<DomainPreconditionFailedException>(() => service.CreateOrder(new CreateOrderInput(
            CustomerId: "CUS-001",
            ShippingAddress: "123 Cloud St",
            Items:
            [
                new CreateOrderItemInput("PRD-002", 1),
                new CreateOrderItemInput("PRD-001", 99999)
            ])));
        Assert.Equal(before2.StockQuantity, products.GetById("PRD-002").StockQuantity);

        // Inactive product PRD-059 cannot be ordered
        Assert.Throws<DomainPreconditionFailedException>(() => service.CreateOrder(new CreateOrderInput(
            CustomerId: "CUS-001",
            ShippingAddress: "123 Cloud St",
            Items: [new CreateOrderItemInput("PRD-059", 1)])));
    }

    [Fact]
    public void TieredVolumePricingStrategy_AppliesTierVolumeAndMaxCap()
    {
        var (service, _) = CreateSut();

        var q1 = service.CalculateQuote("SKU-ELEC-001", 10, CustomerTier.Enterprise, 0m);
        Assert.Equal(20m, q1.DiscountPercent);
        Assert.True(q1.InStock);
        Assert.Equal(q1.LineTotal, q1.RunningSubtotal);

        var q2 = service.CalculateQuote("SKU-BOOK-001", 2, CustomerTier.Standard, q1.RunningSubtotal);
        Assert.Equal(0m, q2.DiscountPercent);
        Assert.Equal(Math.Round(q1.RunningSubtotal + q2.LineTotal, 2), q2.RunningSubtotal);
    }

    [Fact]
    public void PricingStrategy_FollowsOpenClosedPrinciple_WithCustomDiscountRule()
    {
        var customStrategy = new TieredVolumePricingStrategy(
            [new CustomerTierDiscountRule(), new VolumeDiscountRule(), new ElectronicsFlashSaleRule()],
            new PricingOptions { MaxCombinedDiscountPercent = 30m });

        var (service, _) = CreateSut(pricingStrategy: customStrategy);
        // Enterprise (15%) + Qty 10 (5%) + FlashSale Electronics (8%) = 28%
        var quote = service.CalculateQuote("SKU-ELEC-001", 10, CustomerTier.Enterprise, 0m);
        Assert.Equal(28m, quote.DiscountPercent);
        Assert.Contains("FlashSale=8%", quote.Note);
    }
}
