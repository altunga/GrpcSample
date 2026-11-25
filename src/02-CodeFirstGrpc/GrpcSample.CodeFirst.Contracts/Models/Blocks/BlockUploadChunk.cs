using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Blocks;

[DataContract]
public sealed class BlockUploadChunk
{
    [DataMember(Order = 1)] public string? BlockName { get; set; }
    [DataMember(Order = 2)] public string? ContentType { get; set; }
    [DataMember(Order = 3)] public byte[]? ChunkData { get; set; }
}
