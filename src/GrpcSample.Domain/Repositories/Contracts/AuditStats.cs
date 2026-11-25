using GrpcSample.Domain.Models.Audit;

namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record AuditStats(int TotalRequests, IReadOnlyList<AuditEntry> RecentEntries);
