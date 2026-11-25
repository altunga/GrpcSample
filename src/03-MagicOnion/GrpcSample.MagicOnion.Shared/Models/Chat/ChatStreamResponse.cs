using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Chat;

[MessagePackObject]
public sealed class ChatStreamResponse
{
    [Key(0)] public string MessageId { get; set; } = string.Empty;
    [Key(1)] public string RoomId { get; set; } = string.Empty;
    [Key(2)] public string Sender { get; set; } = string.Empty;
    [Key(3)] public string Text { get; set; } = string.Empty;
    [Key(4)] public bool IsSystem { get; set; }
    [Key(5)] public DateTime SentAtUtc { get; set; }
}
