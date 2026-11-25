using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Telemetry;

[DataContract]
public sealed class TelemetryBatchSummaryModel
{
    [DataMember(Order = 1)] public int AcceptedCount { get; set; }
    [DataMember(Order = 2)] public int AnomalyCount { get; set; }
    [DataMember(Order = 3)] public double AverageTemperatureCelsius { get; set; }
    [DataMember(Order = 4)] public double MaxCpuLoadPercent { get; set; }
    [DataMember(Order = 5)] public List<string> AffectedWarehouses { get; set; } = [];
}
