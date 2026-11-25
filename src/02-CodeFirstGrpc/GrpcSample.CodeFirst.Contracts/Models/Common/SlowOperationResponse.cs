using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Common;

[DataContract]
public sealed class SlowOperationResponse
{
    [DataMember(Order = 1)] public string Label { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int ActualDelayMs { get; set; }
    [DataMember(Order = 3)] public string ServerEngine { get; set; } = string.Empty;
}
