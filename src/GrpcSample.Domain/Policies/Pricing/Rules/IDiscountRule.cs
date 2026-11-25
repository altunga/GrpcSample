namespace GrpcSample.Domain.Policies.Pricing.Rules;

public interface IDiscountRule
{
    string Name { get; }
    decimal CalculateDiscountPercent(DiscountContext context);
}
