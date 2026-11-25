using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Blocks;

[MessagePackObject]
public sealed class BlockDownloadChunk
{
    [Key(0)] public string BlockId { get; set; } = string.Empty;
    [Key(1)] public string BlockName { get; set; } = string.Empty;
    [Key(2)] public int Sequence { get; set; }
    [Key(3)] public bool IsLast { get; set; }
    [Key(4)] public byte[] ChunkData { get; set; } = [];
    [Key(5)] public string ExpectedSha256Hex { get; set; } = string.Empty;
}
