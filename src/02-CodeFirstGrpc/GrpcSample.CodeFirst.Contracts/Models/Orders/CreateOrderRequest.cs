using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class CreateOrderRequest
{
    [DataMember(Order = 1)] public string CustomerId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string ShippingAddress { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<CreateOrderLineRequest> Items { get; set; } = [];
}
