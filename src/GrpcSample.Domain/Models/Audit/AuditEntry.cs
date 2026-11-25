namespace GrpcSample.Domain.Models.Audit;

public sealed class AuditEntry
{
    public required string Engine { get; set; }
    public required string Method { get; set; }
    public required string CorrelationId { get; set; }
    public string? User { get; set; }
    public long ElapsedMs { get; set; }
    public required string Status { get; set; }
    public DateTime TimestampUtc { get; set; }
}
