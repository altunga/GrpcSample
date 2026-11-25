namespace GrpcSample.Domain.Chat;

public interface IOrderChatProcessor
{
    ChatCommandReply Process(ChatCommandInput input);
}
