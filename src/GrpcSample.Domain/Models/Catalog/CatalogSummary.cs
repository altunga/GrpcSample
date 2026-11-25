namespace GrpcSample.Domain.Models.Catalog;

public sealed class CatalogSummary
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int TotalStockUnits { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTelemetryReadings { get; set; }
    public int TotalBlocks { get; set; }
    public Dictionary<string, int> ProductsByCategory { get; set; } = [];
}
