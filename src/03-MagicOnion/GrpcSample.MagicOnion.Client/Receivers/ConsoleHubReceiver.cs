using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Hubs;

namespace GrpcSample.MagicOnion.Client.Receivers;

internal sealed class ConsoleHubReceiver : ICommerceHubReceiver
{
    public void OnUserJoined(string roomName, string userName) =>
        Console.WriteLine($"[Hub Event] {userName} joined {roomName}");

    public void OnUserLeft(string roomName, string userName) =>
        Console.WriteLine($"[Hub Event] {userName} left {roomName}");

    public void OnMessageReceived(string roomName, string userName, string message, DateTime sentAtUtc) =>
        Console.WriteLine($"[Hub Event] [{roomName}] {userName}: {message}");

    public void OnOrderStatusChanged(string orderId, string oldStatus, string newStatus, string updatedBy) =>
        Console.WriteLine($"[Hub Event] Order {orderId}: {oldStatus} -> {newStatus} by {updatedBy}");
}
