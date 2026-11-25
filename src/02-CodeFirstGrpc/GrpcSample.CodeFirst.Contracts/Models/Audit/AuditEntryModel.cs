using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Audit;

[DataContract]
public sealed class AuditEntryModel
{
    [DataMember(Order = 1)] public string Engine { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Method { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string CorrelationId { get; set; } = string.Empty;
    [DataMember(Order = 4)] public string User { get; set; } = string.Empty;
    [DataMember(Order = 5)] public long ElapsedMs { get; set; }
    [DataMember(Order = 6)] public string Status { get; set; } = string.Empty;
    [DataMember(Order = 7)] public DateTime TimestampUtc { get; set; }
}
