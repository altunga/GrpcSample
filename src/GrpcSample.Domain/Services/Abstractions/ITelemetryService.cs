using GrpcSample.Domain.Models.Telemetry;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Services.Abstractions;

public interface ITelemetryService
{
    IReadOnlyList<TelemetryReading> GetWarehouseTelemetry(string warehouseId, bool anomaliesOnly, int limit);
    TelemetryBatchSummary IngestBatch(IEnumerable<TelemetryReading> readings);
}
