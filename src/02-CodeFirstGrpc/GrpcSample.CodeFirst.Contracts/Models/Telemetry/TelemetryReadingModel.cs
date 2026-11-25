using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Telemetry;

[DataContract]
public sealed class TelemetryReadingModel
{
    [DataMember(Order = 1)] public string ReadingId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string WarehouseId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string SensorId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public double TemperatureCelsius { get; set; }
    [DataMember(Order = 5)] public double HumidityPercent { get; set; }
    [DataMember(Order = 6)] public double CpuLoadPercent { get; set; }
    [DataMember(Order = 7)] public bool IsAnomaly { get; set; }
    [DataMember(Order = 8)] public DateTime RecordedAtUtc { get; set; }
}
