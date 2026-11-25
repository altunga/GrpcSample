using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Telemetry;

namespace GrpcSample.Domain.Policies.Telemetry;

public interface ITelemetryAnomalyDetector
{
    bool IsAnomaly(TelemetryReading reading);
}
