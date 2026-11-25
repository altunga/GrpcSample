namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record TelemetryBatchSummary(
    int AcceptedCount,
    int AnomalyCount,
    double AverageTemperatureCelsius,
    double MaxCpuLoadPercent,
    IReadOnlyList<string> AffectedWarehouses);
