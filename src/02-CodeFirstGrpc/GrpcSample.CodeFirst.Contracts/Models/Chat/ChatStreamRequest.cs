using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Chat;

[DataContract]
public sealed class ChatStreamRequest
{
    [DataMember(Order = 1)] public string RoomId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Sender { get; set; } = string.Empty;
    [DataMember(Order = 3)] public ChatCommandType Command { get; set; }
    [DataMember(Order = 4)] public string Text { get; set; } = string.Empty;
    [DataMember(Order = 5)] public string? OrderId { get; set; }
}
