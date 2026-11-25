using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Audit;

[MessagePackObject]
public sealed class AdminAuditInfoResponse
{
    [Key(0)] public int TotalRequests { get; set; }
    [Key(1)] public string AuthenticatedUser { get; set; } = string.Empty;
    [Key(2)] public string Role { get; set; } = string.Empty;
    [Key(3)] public List<AuditEntryModel> RecentEntries { get; set; } = [];
}
