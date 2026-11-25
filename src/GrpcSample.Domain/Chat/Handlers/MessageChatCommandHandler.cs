using GrpcSample.Domain.Models.Chat;
using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Chat.Handlers;

public sealed class MessageChatCommandHandler(IChatRepository chatRepository, TimeProvider timeProvider) : IChatCommandHandler
{
    public ChatCommandKind Command => ChatCommandKind.Message;

    public ChatCommandReply Handle(ChatCommandInput input)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var stored = new ChatRoomMessage
        {
            MessageId = $"MSG-{Guid.NewGuid():N}"[..12],
            RoomId = input.RoomId,
            Sender = input.Sender,
            Content = input.Text,
            SentAtUtc = now
        };
        chatRepository.AddMessage(stored);

        return new ChatCommandReply(
            MessageId: stored.MessageId,
            RoomId: input.RoomId,
            Sender: input.Sender,
            Text: $"Echo[{input.RoomId}]: {input.Text}",
            IsSystem: false,
            SentAtUtc: now);
    }
}
