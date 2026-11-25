using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Common;

[DataContract]
public sealed class SlowOperationRequest
{
    [DataMember(Order = 1)] public int DelayMs { get; set; }
    [DataMember(Order = 2)] public string Label { get; set; } = string.Empty;
}
