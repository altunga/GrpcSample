using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Audit;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.Domain.Repositories.Contracts;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryAuditRepository(InMemoryCommerceStore store) : IAuditRepository
{
    public void Record(AuditEntry entry)
    {
        store.IncrementAuditCount();
        store.AuditEntries.Enqueue(entry);
        while (store.AuditEntries.Count > 200 && store.AuditEntries.TryDequeue(out _)) { }
    }

    public AuditStats GetStats() =>
        new(store.TotalAuditCount, store.AuditEntries.TakeLast(25).ToList());
}
