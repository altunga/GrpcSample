using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Pricing;
using GrpcSample.Domain.Models.Products;

namespace GrpcSample.Domain.Policies.Pricing;

public interface IPricingStrategy
{
    QuoteResult CalculateQuote(Product product, int quantity, CustomerTier tier, decimal currentSubtotal);
}
