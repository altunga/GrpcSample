using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Telemetry;

[DataContract]
public sealed class TelemetrySubscriptionRequest
{
    [DataMember(Order = 1)] public string WarehouseId { get; set; } = "*";
    [DataMember(Order = 2)] public bool AnomaliesOnly { get; set; }
    [DataMember(Order = 3)] public int MaxReadings { get; set; } = 25;
    [DataMember(Order = 4)] public int IntervalMs { get; set; }
}
