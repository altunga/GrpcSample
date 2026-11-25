using GrpcSample.Domain.Models.Audit;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.Contracts;
using GrpcSample.Domain.Services.Abstractions;

namespace GrpcSample.Domain.Services.Implementations;

public sealed class AuditService(IAuditRepository auditRepository, TimeProvider timeProvider) : IAuditService
{
    public void Record(string engine, string method, string correlationId, string? user, long elapsedMs, string status) =>
        auditRepository.Record(new AuditEntry
        {
            Engine = engine,
            Method = method,
            CorrelationId = correlationId,
            User = user,
            ElapsedMs = elapsedMs,
            Status = status,
            TimestampUtc = timeProvider.GetUtcNow().UtcDateTime
        });

    public AuditStats GetStats() => auditRepository.GetStats();
}
