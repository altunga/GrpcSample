using System.Collections.Concurrent;
using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Hubs;
using Xunit;

namespace GrpcSample.MagicOnion.Tests.TestDoubles;

internal sealed class TestHubReceiver : ICommerceHubReceiver
{
    public ConcurrentQueue<string> JoinedUsers { get; } = new();
    public ConcurrentQueue<string> LeftUsers { get; } = new();
    public ConcurrentQueue<string> Messages { get; } = new();
    public ConcurrentQueue<string> OrderUpdates { get; } = new();

    public void OnUserJoined(string roomName, string userName) => JoinedUsers.Enqueue(userName);
    public void OnUserLeft(string roomName, string userName) => LeftUsers.Enqueue(userName);
    public void OnMessageReceived(string roomName, string userName, string message, DateTime sentAtUtc) =>
        Messages.Enqueue($"{userName}: {message}");
    public void OnOrderStatusChanged(string orderId, string oldStatus, string newStatus, string updatedBy) =>
        OrderUpdates.Enqueue($"{orderId}->{newStatus}");

    public async Task WaitForAsync(Func<bool> predicate, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!predicate() && DateTime.UtcNow < deadline)
            await Task.Delay(25);

        Assert.True(predicate(), "Timed out waiting for StreamingHub broadcast event.");
    }
}
