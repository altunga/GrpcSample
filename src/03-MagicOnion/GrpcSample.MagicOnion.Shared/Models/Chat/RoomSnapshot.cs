using MessagePack;

namespace GrpcSample.MagicOnion.Shared.Models.Chat;

[MessagePackObject]
public sealed class RoomSnapshot
{
    [Key(0)] public string RoomName { get; set; } = string.Empty;
    [Key(1)] public string JoinedUser { get; set; } = string.Empty;
    [Key(2)] public List<string> RecentHistory { get; set; } = [];
}
