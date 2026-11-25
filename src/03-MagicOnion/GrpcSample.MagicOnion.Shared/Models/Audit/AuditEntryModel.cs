using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Audit;

[MessagePackObject]
public sealed class AuditEntryModel
{
    [Key(0)] public string Engine { get; set; } = string.Empty;
    [Key(1)] public string Method { get; set; } = string.Empty;
    [Key(2)] public string CorrelationId { get; set; } = string.Empty;
    [Key(3)] public string User { get; set; } = string.Empty;
    [Key(4)] public long ElapsedMs { get; set; }
    [Key(5)] public string Status { get; set; } = string.Empty;
    [Key(6)] public DateTime TimestampUtc { get; set; }
}
