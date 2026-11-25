using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Policies.Pricing;
using GrpcSample.Domain.Policies.Pricing.Rules;

namespace GrpcSample.Domain.Tests.TestDoubles;

internal sealed class ElectronicsFlashSaleRule : IDiscountRule
{
    public string Name => "FlashSale";

    public decimal CalculateDiscountPercent(DiscountContext context) =>
        context.Product.Category == ProductCategory.Electronics ? 8m : 0m;
}
