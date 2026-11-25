namespace GrpcSample.Domain.Models.Products;

public sealed class Product
{
    public required string Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public double Rating { get; set; }
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public Product Clone() => new()
    {
        Id = Id,
        Sku = Sku,
        Name = Name,
        Description = Description,
        Category = Category,
        Price = Price,
        StockQuantity = StockQuantity,
        Rating = Rating,
        Tags = [.. Tags],
        Attributes = new Dictionary<string, string>(Attributes, StringComparer.OrdinalIgnoreCase),
        IsActive = IsActive,
        CreatedAtUtc = CreatedAtUtc
    };
}
