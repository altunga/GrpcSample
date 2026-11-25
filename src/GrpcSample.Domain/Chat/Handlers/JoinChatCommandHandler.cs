using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Chat.Handlers;

public sealed class JoinChatCommandHandler(IChatRepository chatRepository, TimeProvider timeProvider) : IChatCommandHandler
{
    public ChatCommandKind Command => ChatCommandKind.Join;

    public ChatCommandReply Handle(ChatCommandInput input)
    {
        var history = chatRepository.GetRecentMessages(input.RoomId, 3);
        return new ChatCommandReply(
            MessageId: $"SYS-{Guid.NewGuid():N}"[..12],
            RoomId: input.RoomId,
            Sender: "system",
            Text: $"Welcome {input.Sender} to '{input.RoomId}'. Recent messages: {history.Count}.",
            IsSystem: true,
            SentAtUtc: timeProvider.GetUtcNow().UtcDateTime);
    }
}
