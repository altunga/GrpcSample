using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Chat;

[DataContract]
public sealed class ChatStreamResponse
{
    [DataMember(Order = 1)] public string MessageId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string RoomId { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Sender { get; set; } = string.Empty;
    [DataMember(Order = 4)] public string Text { get; set; } = string.Empty;
    [DataMember(Order = 5)] public bool IsSystem { get; set; }
    [DataMember(Order = 6)] public DateTime SentAtUtc { get; set; }
}
