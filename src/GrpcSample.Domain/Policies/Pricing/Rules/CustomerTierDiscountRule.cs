using GrpcSample.Domain.Models.Customers;

namespace GrpcSample.Domain.Policies.Pricing.Rules;

public sealed class CustomerTierDiscountRule : IDiscountRule
{
    public string Name => "Tier";

    public decimal CalculateDiscountPercent(DiscountContext context) => context.Tier switch
    {
        CustomerTier.Silver => 5m,
        CustomerTier.Gold => 10m,
        CustomerTier.Enterprise => 15m,
        _ => 0m
    };
}
