using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Policies.Telemetry;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services.Abstractions;

namespace GrpcSample.Domain.Services.Implementations;

public sealed class TelemetryService(
    ITelemetryRepository telemetryRepository,
    ITelemetryAnomalyDetector anomalyDetector,
    TimeProvider timeProvider) : ITelemetryService
{
    public IReadOnlyList<TelemetryReading> GetWarehouseTelemetry(string warehouseId, bool anomaliesOnly, int limit) =>
        telemetryRepository.GetByWarehouse(warehouseId, anomaliesOnly, limit);

    public TelemetryBatchSummary IngestBatch(IEnumerable<TelemetryReading> readings)
    {
        var normalized = new List<TelemetryReading>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var raw in readings)
        {
            if (string.IsNullOrWhiteSpace(raw.WarehouseId))
                throw new DomainValidationException("Telemetry WarehouseId is required.");

            var candidate = new TelemetryReading
            {
                ReadingId = raw.ReadingId?.Trim() ?? string.Empty,
                WarehouseId = raw.WarehouseId.Trim(),
                SensorId = string.IsNullOrWhiteSpace(raw.SensorId) ? "SENS-01" : raw.SensorId.Trim(),
                TemperatureCelsius = raw.TemperatureCelsius,
                HumidityPercent = raw.HumidityPercent,
                CpuLoadPercent = raw.CpuLoadPercent,
                IsAnomaly = raw.IsAnomaly,
                RecordedAtUtc = raw.RecordedAtUtc == default ? now : raw.RecordedAtUtc
            };
            candidate.IsAnomaly = anomalyDetector.IsAnomaly(candidate);
            normalized.Add(candidate);
        }

        if (normalized.Count == 0)
            throw new DomainValidationException("Telemetry batch cannot be empty.");

        var stored = telemetryRepository.AddRange(normalized);
        return new TelemetryBatchSummary(
            AcceptedCount: stored.Count,
            AnomalyCount: stored.Count(r => r.IsAnomaly),
            AverageTemperatureCelsius: Math.Round(stored.Average(r => r.TemperatureCelsius), 2),
            MaxCpuLoadPercent: Math.Round(stored.Max(r => r.CpuLoadPercent), 2),
            AffectedWarehouses: stored
                .Select(r => r.WarehouseId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList());
    }
}
