namespace GrpcSample.Domain.Chat.Handlers;

public sealed class PingChatCommandHandler(TimeProvider timeProvider) : IChatCommandHandler
{
    public ChatCommandKind Command => ChatCommandKind.Ping;

    public ChatCommandReply Handle(ChatCommandInput input) =>
        new(
            MessageId: $"PNG-{Guid.NewGuid():N}"[..12],
            RoomId: input.RoomId,
            Sender: "system",
            Text: "PONG",
            IsSystem: true,
            SentAtUtc: timeProvider.GetUtcNow().UtcDateTime);
}
