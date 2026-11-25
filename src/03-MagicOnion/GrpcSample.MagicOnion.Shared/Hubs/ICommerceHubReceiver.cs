namespace GrpcSample.MagicOnion.Shared.Hubs;

public interface ICommerceHubReceiver
{
    void OnUserJoined(string roomName, string userName);
    void OnUserLeft(string roomName, string userName);
    void OnMessageReceived(string roomName, string userName, string message, DateTime sentAtUtc);
    void OnOrderStatusChanged(string orderId, string oldStatus, string newStatus, string updatedBy);
}
