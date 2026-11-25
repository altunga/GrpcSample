using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Blocks;

[DataContract]
public sealed class BlockDownloadChunk
{
    [DataMember(Order = 1)] public string BlockId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string BlockName { get; set; } = string.Empty;
    [DataMember(Order = 3)] public int Sequence { get; set; }
    [DataMember(Order = 4)] public bool IsLast { get; set; }
    [DataMember(Order = 5)] public byte[] ChunkData { get; set; } = [];
    [DataMember(Order = 6)] public string ExpectedSha256Hex { get; set; } = string.Empty;
}
