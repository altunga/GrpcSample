using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class CreateOrderLineRequest
{
    [DataMember(Order = 1)] public string ProductId { get; set; } = string.Empty;
    [DataMember(Order = 2)] public int Quantity { get; set; }
}
