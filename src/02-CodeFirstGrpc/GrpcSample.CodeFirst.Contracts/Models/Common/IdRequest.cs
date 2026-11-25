using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Common;

[DataContract]
public sealed class IdRequest
{
    [DataMember(Order = 1)] public string Id { get; set; } = string.Empty;
}
