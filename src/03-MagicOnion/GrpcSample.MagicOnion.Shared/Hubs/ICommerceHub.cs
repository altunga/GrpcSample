using GrpcSample.MagicOnion.Shared.Models.Chat;
using MagicOnion;

namespace GrpcSample.MagicOnion.Shared.Hubs;

public interface ICommerceHub : IStreamingHub<ICommerceHub, ICommerceHubReceiver>
{
    ValueTask<RoomSnapshot> JoinRoomAsync(string roomName, string userName);
    ValueTask LeaveRoomAsync();
    ValueTask SendMessageAsync(string message);
    ValueTask BroadcastOrderUpdateAsync(string orderId, string newStatus);
}
