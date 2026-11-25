using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Chat;
using GrpcSample.Domain.Repositories;
using GrpcSample.Domain.Repositories.Abstractions;
using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Models.Chat;
using GrpcSample.MagicOnion.Shared.Hubs;
using MagicOnion.Server.Hubs;

namespace GrpcSample.MagicOnion.Server.Hubs;

public sealed class CommerceHub(
    IOrderRepository orderRepository,
    IChatRepository chatRepository,
    TimeProvider timeProvider)
    : StreamingHubBase<ICommerceHub, ICommerceHubReceiver>, ICommerceHub
{
    private IGroup<ICommerceHubReceiver>? _room;
    private string _roomName = string.Empty;
    private string _userName = string.Empty;

    public async ValueTask<RoomSnapshot> JoinRoomAsync(string roomName, string userName)
    {
        _roomName = string.IsNullOrWhiteSpace(roomName) ? "order-support" : roomName.Trim();
        _userName = string.IsNullOrWhiteSpace(userName) ? "anonymous" : userName.Trim();
        _room = await Group.AddAsync(_roomName);

        _room.All.OnUserJoined(_roomName, _userName);

        var history = chatRepository.GetRecentMessages(_roomName, 10)
            .Select(m => $"{m.Sender}: {m.Content}")
            .ToList();

        return new RoomSnapshot
        {
            RoomName = _roomName,
            JoinedUser = _userName,
            RecentHistory = history
        };
    }

    public async ValueTask LeaveRoomAsync()
    {
        if (_room is not null)
        {
            var leftUser = _userName;
            var room = _roomName;
            await _room.RemoveAsync(Context);
            _room.All.OnUserLeft(room, leftUser);
            _room = null;
        }
    }

    public ValueTask SendMessageAsync(string message)
    {
        if (_room is null)
            return ValueTask.CompletedTask;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        chatRepository.AddMessage(new ChatRoomMessage
        {
            MessageId = $"HUB-{Guid.NewGuid():N}"[..12],
            RoomId = _roomName,
            Sender = _userName,
            Content = message,
            SentAtUtc = now
        });

        _room.All.OnMessageReceived(_roomName, _userName, message, now);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastOrderUpdateAsync(string orderId, string newStatus)
    {
        if (_room is null)
            return ValueTask.CompletedTask;

        var order = orderRepository.GetById(orderId);
        var oldStatus = order.Status.ToString();
        _room.All.OnOrderStatusChanged(order.Id, oldStatus, newStatus, _userName);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnDisconnected()
    {
        if (_room is not null && !string.IsNullOrWhiteSpace(_userName))
            _room.All.OnUserLeft(_roomName, _userName);

        return ValueTask.CompletedTask;
    }
}
