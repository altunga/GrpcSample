using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Common;

[DataContract]
public sealed class DeleteResponse
{
    [DataMember(Order = 1)] public bool Deleted { get; set; }
    [DataMember(Order = 2)] public string Id { get; set; } = string.Empty;
}
