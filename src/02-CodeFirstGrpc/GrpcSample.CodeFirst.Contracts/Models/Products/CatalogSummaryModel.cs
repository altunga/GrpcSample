using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Products;

[DataContract]
public sealed class CatalogSummaryModel
{
    [DataMember(Order = 1)] public int TotalProducts { get; set; }
    [DataMember(Order = 2)] public int ActiveProducts { get; set; }
    [DataMember(Order = 3)] public int TotalStockUnits { get; set; }
    [DataMember(Order = 4)] public decimal TotalInventoryValue { get; set; }
    [DataMember(Order = 5)] public int TotalCustomers { get; set; }
    [DataMember(Order = 6)] public int TotalOrders { get; set; }
    [DataMember(Order = 7)] public decimal TotalRevenue { get; set; }
    [DataMember(Order = 8)] public int TotalTelemetryReadings { get; set; }
    [DataMember(Order = 9)] public int TotalBlocks { get; set; }
    [DataMember(Order = 10)] public Dictionary<string, int> ProductsByCategory { get; set; } = [];
}
