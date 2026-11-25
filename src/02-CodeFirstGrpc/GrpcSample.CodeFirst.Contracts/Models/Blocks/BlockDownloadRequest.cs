using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Blocks;

[DataContract]
public sealed class BlockDownloadRequest
{
    [DataMember(Order = 1)] public string BlockIdOrName { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int ChunkSize { get; set; } = 4096;
}
