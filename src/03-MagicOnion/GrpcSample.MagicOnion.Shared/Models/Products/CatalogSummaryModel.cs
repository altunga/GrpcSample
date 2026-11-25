using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Products;

[MessagePackObject]
public sealed class CatalogSummaryModel
{
    [Key(0)] public int TotalProducts { get; set; }
    [Key(1)] public int ActiveProducts { get; set; }
    [Key(2)] public int TotalStockUnits { get; set; }
    [Key(3)] public decimal TotalInventoryValue { get; set; }
    [Key(4)] public int TotalCustomers { get; set; }
    [Key(5)] public int TotalOrders { get; set; }
    [Key(6)] public decimal TotalRevenue { get; set; }
    [Key(7)] public int TotalTelemetryReadings { get; set; }
    [Key(8)] public int TotalBlocks { get; set; }
    [Key(9)] public Dictionary<string, int> ProductsByCategory { get; set; } = [];
}
