using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Chat;

[MessagePackObject]
public sealed class ChatStreamRequest
{
    [Key(0)] public string RoomId { get; set; } = string.Empty;
    [Key(1)] public string Sender { get; set; } = string.Empty;
    [Key(2)] public ChatCommandType Command { get; set; }
    [Key(3)] public string Text { get; set; } = string.Empty;
    [Key(4)] public string? OrderId { get; set; }
}
