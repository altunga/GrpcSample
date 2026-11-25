namespace GrpcSample.Domain.Models.Telemetry;

public sealed class TelemetryReading
{
    public required string ReadingId { get; set; }
    public required string WarehouseId { get; set; }
    public required string SensorId { get; set; }
    public double TemperatureCelsius { get; set; }
    public double HumidityPercent { get; set; }
    public double CpuLoadPercent { get; set; }
    public bool IsAnomaly { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
