namespace GrpcSample.Domain.Configuration;

public sealed class TelemetryAnomalyOptions
{
    public double MaxTemperatureCelsius { get; set; } = 30.0;
    public double MaxCpuLoadPercent { get; set; } = 85.0;
}
