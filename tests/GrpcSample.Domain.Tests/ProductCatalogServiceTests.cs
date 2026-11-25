using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Repositories.InMemory;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Implementations;
using GrpcSample.Domain.Tests.TestDoubles;
using Xunit;

namespace GrpcSample.Domain.Tests;

public sealed class ProductCatalogServiceTests
{
    private static (ProductCatalogService Service, InMemoryCommerceStore Store) CreateSut(TimeProvider? time = null)
    {
        var store = InMemoryCommerceStore.CreateSeeded();
        var service = new ProductCatalogService(
            new InMemoryProductRepository(store),
            new InMemoryCustomerRepository(store),
            new InMemoryOrderRepository(store),
            new InMemoryTelemetryRepository(store),
            new InMemoryBlockRepository(store),
            time ?? TimeProvider.System);
        return (service, store);
    }

    [Fact]
    public void SeededCatalogSummary_ReflectsAllSeedInvariants()
    {
        var (service, _) = CreateSut();
        var summary = service.GetCatalogSummary();

        Assert.Equal(60, summary.TotalProducts);
        Assert.Equal(59, summary.ActiveProducts);
        Assert.Equal(25, summary.TotalCustomers);
        Assert.Equal(35, summary.TotalOrders);
        Assert.Equal(120, summary.TotalTelemetryReadings);
        Assert.Equal(16, summary.TotalBlocks);
        Assert.Equal(6, summary.ProductsByCategory.Count);
        Assert.True(summary.TotalInventoryValue > 0);
        Assert.True(summary.TotalRevenue > 0);
    }

    [Fact]
    public void Crud_And_Filters_WorkEndToEnd_WithDeterministicTimeProvider()
    {
        var fixedNow = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var (service, _) = CreateSut(new FixedTimeProvider(fixedNow));

        var fetched = service.GetProduct("PRD-001");
        Assert.Equal("SKU-ELEC-001", fetched.Sku);

        var paged = service.ListProducts(new ProductFilter(
            Category: ProductCategory.Electronics,
            ActiveOnly: true,
            Page: 1,
            PageSize: 5));
        Assert.Equal(10, paged.TotalCount);
        Assert.Equal(5, paged.Items.Count);

        var created = service.CreateProduct(new CreateProductInput(
            Sku: "SKU-TEST-999",
            Name: "Test Hardware",
            Description: "Domain unit test product",
            Category: ProductCategory.Electronics,
            Price: 150.25m,
            StockQuantity: 20,
            Rating: 4.8,
            Tags: ["unit"],
            Attributes: new Dictionary<string, string> { ["color"] = "black" }));
        Assert.Equal("PRD-061", created.Id);
        Assert.Equal(fixedNow.UtcDateTime, created.CreatedAtUtc);

        var updated = service.UpdateProduct(new UpdateProductInput(
            Id: created.Id,
            Name: "Updated Hardware",
            Description: "Updated description",
            Category: ProductCategory.HomeOffice,
            Price: 175.00m,
            StockQuantity: 18,
            IsActive: true,
            Tags: ["updated"],
            Attributes: null));
        Assert.Equal("Updated Hardware", updated.Name);
        Assert.Equal(ProductCategory.HomeOffice, updated.Category);
        Assert.Equal(175.00m, updated.Price);

        Assert.True(service.DeleteProduct(created.Id));
        Assert.Throws<DomainNotFoundException>(() => service.GetProduct(created.Id));
    }

    [Fact]
    public void CreateProduct_ThrowsExpectedDomainExceptions()
    {
        var (service, _) = CreateSut();

        Assert.Throws<DomainAlreadyExistsException>(() => service.CreateProduct(new CreateProductInput(
            "SKU-ELEC-001", "Duplicate", "Desc", ProductCategory.Electronics, 10m, 5, 4.0, null, null)));

        Assert.Throws<DomainValidationException>(() => service.CreateProduct(new CreateProductInput(
            "", "Invalid", "Desc", ProductCategory.Electronics, 10m, 5, 4.0, null, null)));

        Assert.Throws<DomainValidationException>(() => service.CreateProduct(new CreateProductInput(
            "SKU-NEG-01", "Invalid Price", "Desc", ProductCategory.Electronics, 0m, 5, 4.0, null, null)));
    }

    [Fact]
    public async Task ParallelReadsAndCreates_AreThreadSafe()
    {
        var (service, _) = CreateSut();
        var tasks = Enumerable.Range(1, 20).Select(i => Task.Run(() =>
        {
            var created = service.CreateProduct(new CreateProductInput(
                $"SKU-PAR-{i:000}", $"Parallel-{i}", "Thread safety", ProductCategory.Accessories, 19.99m, 50, 4.5, null, null));
            var fetched = service.GetProduct(created.Id);
            Assert.Equal(created.Sku, fetched.Sku);
        }));

        await Task.WhenAll(tasks);
        Assert.Equal(80, service.GetCatalogSummary().TotalProducts);
    }
}
