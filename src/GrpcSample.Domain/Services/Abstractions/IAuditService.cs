using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Services.Abstractions;

public interface IAuditService
{
    void Record(string engine, string method, string correlationId, string? user, long elapsedMs, string status);
    AuditStats GetStats();
}
