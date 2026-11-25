using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Telemetry;

[MessagePackObject]
public sealed class TelemetryReadingModel
{
    [Key(0)] public string ReadingId { get; set; } = string.Empty;
    [Key(1)] public string WarehouseId { get; set; } = string.Empty;
    [Key(2)] public string SensorId { get; set; } = string.Empty;
    [Key(3)] public double TemperatureCelsius { get; set; }
    [Key(4)] public double HumidityPercent { get; set; }
    [Key(5)] public double CpuLoadPercent { get; set; }
    [Key(6)] public bool IsAnomaly { get; set; }
    [Key(7)] public DateTime RecordedAtUtc { get; set; }
}
