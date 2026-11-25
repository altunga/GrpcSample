using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Blocks;

[DataContract]
public sealed class BlockUploadResult
{
    [DataMember(Order = 1)] public string BlockId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string BlockName { get; set; } = string.Empty;
    [DataMember(Order = 3)] public long TotalBytes { get; set; }
    [DataMember(Order = 4)] public int ChunkCount { get; set; }
    [DataMember(Order = 5)] public string Sha256Hex { get; set; } = string.Empty;
}
