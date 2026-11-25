using GrpcSample.Domain.Configuration;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Telemetry;

namespace GrpcSample.Domain.Policies.Telemetry;

public sealed class ThresholdTelemetryAnomalyDetector(TelemetryAnomalyOptions options) : ITelemetryAnomalyDetector
{
    public bool IsAnomaly(TelemetryReading reading) =>
        reading.IsAnomaly ||
        reading.TemperatureCelsius > options.MaxTemperatureCelsius ||
        reading.CpuLoadPercent > options.MaxCpuLoadPercent;
}
