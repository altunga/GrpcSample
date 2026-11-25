using GrpcSample.Domain.Models.Telemetry;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface ITelemetryRepository
{
    IReadOnlyList<TelemetryReading> GetByWarehouse(string warehouseId, bool anomaliesOnly, int limit);
    IReadOnlyList<TelemetryReading> AddRange(IEnumerable<TelemetryReading> readings);
    int Count();
}
