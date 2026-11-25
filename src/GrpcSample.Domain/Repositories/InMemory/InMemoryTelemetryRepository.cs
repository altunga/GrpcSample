using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryTelemetryRepository(InMemoryCommerceStore store) : ITelemetryRepository
{
    public IReadOnlyList<TelemetryReading> GetByWarehouse(string warehouseId, bool anomaliesOnly, int limit)
    {
        var max = Math.Clamp(limit <= 0 ? 25 : limit, 1, 500);
        IEnumerable<TelemetryReading> query = store.Telemetry;

        if (!string.IsNullOrWhiteSpace(warehouseId) && !string.Equals(warehouseId, "*", StringComparison.Ordinal))
            query = query.Where(r => string.Equals(r.WarehouseId, warehouseId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (anomaliesOnly)
            query = query.Where(r => r.IsAnomaly);

        return query
            .OrderBy(r => r.ReadingId, StringComparer.Ordinal)
            .Take(max)
            .ToList();
    }

    public IReadOnlyList<TelemetryReading> AddRange(IEnumerable<TelemetryReading> readings)
    {
        var saved = new List<TelemetryReading>();
        foreach (var item in readings)
        {
            if (string.IsNullOrWhiteSpace(item.ReadingId))
                item.ReadingId = store.NextTelemetryId();

            store.Telemetry.Enqueue(item);
            saved.Add(item);
        }
        return saved;
    }

    public int Count() => store.Telemetry.Count;
}
