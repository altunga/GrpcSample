namespace GrpcSample.Domain.Chat;

public enum ChatCommandKind
{
    Join = 0,
    Message = 1,
    OrderStatusQuery = 2,
    Ping = 3
}
