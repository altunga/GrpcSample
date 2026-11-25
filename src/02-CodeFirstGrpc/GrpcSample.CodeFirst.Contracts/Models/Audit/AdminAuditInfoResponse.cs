using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Audit;

[DataContract]
public sealed class AdminAuditInfoResponse
{
    [DataMember(Order = 1)] public int TotalRequests { get; set; }
    [DataMember(Order = 2)] public string AuthenticatedUser { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Role { get; set; } = string.Empty;
    [DataMember(Order = 4)] public List<AuditEntryModel> RecentEntries { get; set; } = [];
}
