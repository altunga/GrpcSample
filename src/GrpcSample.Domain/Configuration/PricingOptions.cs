namespace GrpcSample.Domain.Configuration;

public sealed class PricingOptions
{
    public decimal MaxCombinedDiscountPercent { get; set; } = 25m;
}
