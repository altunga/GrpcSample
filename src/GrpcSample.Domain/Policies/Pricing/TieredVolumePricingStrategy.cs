using GrpcSample.Domain.Configuration;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Pricing;
using GrpcSample.Domain.Models.Products;
using GrpcSample.Domain.Policies.Pricing.Rules;

namespace GrpcSample.Domain.Policies.Pricing;

public sealed class TieredVolumePricingStrategy(
    IEnumerable<IDiscountRule> rules,
    PricingOptions options) : IPricingStrategy
{
    private readonly IReadOnlyList<IDiscountRule> _rules = rules.ToList();

    public QuoteResult CalculateQuote(Product product, int quantity, CustomerTier tier, decimal currentSubtotal)
    {
        var context = new DiscountContext(product, quantity, tier);
        decimal rawDiscount = 0m;
        var appliedNotes = new List<string>(_rules.Count);

        foreach (var rule in _rules)
        {
            var pct = Math.Max(0m, rule.CalculateDiscountPercent(context));
            rawDiscount += pct;
            appliedNotes.Add($"{rule.Name}={pct}%");
        }

        decimal totalDiscount = Math.Min(options.MaxCombinedDiscountPercent, rawDiscount);
        decimal lineTotal = Math.Round(product.Price * quantity * (1m - totalDiscount / 100m), 2);
        decimal runningSubtotal = Math.Round(currentSubtotal + lineTotal, 2);
        bool inStock = product.IsActive && product.StockQuantity >= quantity;

        return new QuoteResult
        {
            Sku = product.Sku,
            ProductName = product.Name,
            RequestedQuantity = quantity,
            UnitPrice = product.Price,
            DiscountPercent = totalDiscount,
            LineTotal = lineTotal,
            RunningSubtotal = runningSubtotal,
            InStock = inStock,
            Note = inStock
                ? $"Tier={tier} ({string.Join(", ", appliedNotes)})"
                : $"Insufficient stock (available={product.StockQuantity})"
        };
    }
}
