namespace GrpcSample.Domain.Chat;

public sealed record ChatCommandInput(
    string RoomId,
    string Sender,
    ChatCommandKind Command,
    string Text,
    string? OrderId);
