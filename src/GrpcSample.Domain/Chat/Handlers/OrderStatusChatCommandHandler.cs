using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Chat.Handlers;

public sealed class OrderStatusChatCommandHandler(IOrderRepository orderRepository, TimeProvider timeProvider) : IChatCommandHandler
{
    public ChatCommandKind Command => ChatCommandKind.OrderStatusQuery;

    public ChatCommandReply Handle(ChatCommandInput input)
    {
        var orderId = !string.IsNullOrWhiteSpace(input.OrderId) ? input.OrderId : input.Text;
        var order = orderRepository.GetById(orderId);
        return new ChatCommandReply(
            MessageId: $"ORD-{Guid.NewGuid():N}"[..12],
            RoomId: input.RoomId,
            Sender: "order-bot",
            Text: $"Order {order.Id} status={order.Status}, total={order.TotalAmount:F2}, tracking={order.TrackingNumber ?? "N/A"}",
            IsSystem: true,
            SentAtUtc: timeProvider.GetUtcNow().UtcDateTime);
    }
}
