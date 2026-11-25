using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Models.Products;

namespace GrpcSample.Domain.Policies.Pricing;

public sealed record DiscountContext(Product Product, int Quantity, CustomerTier Tier);
