namespace GrpcSample.Domain.Chat;

public sealed record ChatCommandReply(
    string MessageId,
    string RoomId,
    string Sender,
    string Text,
    bool IsSystem,
    DateTime SentAtUtc);
