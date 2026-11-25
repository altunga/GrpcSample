using GrpcSample.Domain.Models.Audit;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface IAuditRepository
{
    void Record(AuditEntry entry);
    AuditStats GetStats();
}
