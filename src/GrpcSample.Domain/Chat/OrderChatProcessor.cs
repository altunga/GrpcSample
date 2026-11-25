using GrpcSample.Domain.Chat.Handlers;

namespace GrpcSample.Domain.Chat;

public sealed class OrderChatProcessor(IEnumerable<IChatCommandHandler> handlers) : IOrderChatProcessor
{
    private readonly IReadOnlyDictionary<ChatCommandKind, IChatCommandHandler> _handlers =
        handlers.ToDictionary(h => h.Command);

    public ChatCommandReply Process(ChatCommandInput input)
    {
        var normalized = input with
        {
            RoomId = string.IsNullOrWhiteSpace(input.RoomId) ? "order-support" : input.RoomId.Trim(),
            Sender = string.IsNullOrWhiteSpace(input.Sender) ? "guest" : input.Sender.Trim()
        };

        var handler = _handlers.TryGetValue(normalized.Command, out var matched)
            ? matched
            : _handlers[ChatCommandKind.Message];

        return handler.Handle(normalized);
    }
}
