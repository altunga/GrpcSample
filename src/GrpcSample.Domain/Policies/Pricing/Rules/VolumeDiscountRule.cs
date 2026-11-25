namespace GrpcSample.Domain.Policies.Pricing.Rules;

public sealed class VolumeDiscountRule : IDiscountRule
{
    public string Name => "Volume";

    public decimal CalculateDiscountPercent(DiscountContext context) =>
        context.Quantity >= 10 ? 5m : (context.Quantity >= 5 ? 2.5m : 0m);
}
