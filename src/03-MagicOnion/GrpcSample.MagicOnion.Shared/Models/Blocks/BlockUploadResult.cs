using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Blocks;

[MessagePackObject]
public sealed class BlockUploadResult
{
    [Key(0)] public string BlockId { get; set; } = string.Empty;
    [Key(1)] public string BlockName { get; set; } = string.Empty;
    [Key(2)] public long TotalBytes { get; set; }
    [Key(3)] public int ChunkCount { get; set; }
    [Key(4)] public string Sha256Hex { get; set; } = string.Empty;
}
