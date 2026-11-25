using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Common;

[MessagePackObject]
public sealed class DeleteResponse
{
    [Key(0)] public bool Deleted { get; set; }
    [Key(1)] public string Id { get; set; } = string.Empty;
}
