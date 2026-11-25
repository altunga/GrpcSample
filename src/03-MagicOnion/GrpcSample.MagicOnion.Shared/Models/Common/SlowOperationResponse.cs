using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Common;

[MessagePackObject]
public sealed class SlowOperationResponse
{
    [Key(0)] public string Label { get; set; } = string.Empty;
    [Key(1)] public int ActualDelayMs { get; set; }
    [Key(2)] public string ServerEngine { get; set; } = string.Empty;
}
