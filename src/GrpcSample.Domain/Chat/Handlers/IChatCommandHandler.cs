namespace GrpcSample.Domain.Chat.Handlers;

public interface IChatCommandHandler
{
    ChatCommandKind Command { get; }
    ChatCommandReply Handle(ChatCommandInput input);
}
