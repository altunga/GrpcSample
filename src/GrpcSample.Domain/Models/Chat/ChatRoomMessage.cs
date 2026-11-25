namespace GrpcSample.Domain.Models.Chat;

public sealed class ChatRoomMessage
{
    public required string MessageId { get; set; }
    public required string RoomId { get; set; }
    public required string Sender { get; set; }
    public required string Content { get; set; }
    public DateTime SentAtUtc { get; set; }
}
