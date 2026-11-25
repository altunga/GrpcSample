namespace GrpcSample.Domain.Models.Customers;

public sealed class Customer
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public CustomerTier Tier { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
