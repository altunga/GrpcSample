using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Telemetry;

[MessagePackObject]
public sealed class TelemetrySubscriptionRequest
{
    [Key(0)] public string WarehouseId { get; set; } = "*";
    [Key(1)] public bool AnomaliesOnly { get; set; }
    [Key(2)] public int MaxReadings { get; set; } = 25;
    [Key(3)] public int IntervalMs { get; set; }
}
