using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Chat;
using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryChatRepository(InMemoryCommerceStore store) : IChatRepository
{
    public void AddMessage(ChatRoomMessage message) => store.ChatMessages.Enqueue(message);

    public IReadOnlyList<ChatRoomMessage> GetRecentMessages(string roomId, int limit = 20) =>
        store.ChatMessages
            .Where(m => string.Equals(m.RoomId, roomId, StringComparison.OrdinalIgnoreCase))
            .TakeLast(Math.Clamp(limit, 1, 100))
            .ToList();
}
