using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Blocks;

[MessagePackObject]
public sealed class BlockUploadChunk
{
    [Key(0)] public string? BlockName { get; set; }
    [Key(1)] public string? ContentType { get; set; }
    [Key(2)] public byte[]? ChunkData { get; set; }
}
