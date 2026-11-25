using GrpcSample.Domain.Models.Chat;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface IChatRepository
{
    void AddMessage(ChatRoomMessage message);
    IReadOnlyList<ChatRoomMessage> GetRecentMessages(string roomId, int limit = 20);
}
