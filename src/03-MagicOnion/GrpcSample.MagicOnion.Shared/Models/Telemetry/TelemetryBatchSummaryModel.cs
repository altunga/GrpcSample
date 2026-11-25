using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Telemetry;

[MessagePackObject]
public sealed class TelemetryBatchSummaryModel
{
    [Key(0)] public int AcceptedCount { get; set; }
    [Key(1)] public int AnomalyCount { get; set; }
    [Key(2)] public double AverageTemperatureCelsius { get; set; }
    [Key(3)] public double MaxCpuLoadPercent { get; set; }
    [Key(4)] public List<string> AffectedWarehouses { get; set; } = [];
}
