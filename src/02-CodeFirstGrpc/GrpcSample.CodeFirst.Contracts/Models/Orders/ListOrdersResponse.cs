using System.Runtime.Serialization;

namespace GrpcSample.CodeFirst.Contracts.Models.Orders;

[DataContract]
public sealed class ListOrdersResponse
{
    [DataMember(Order = 1)] public List<OrderModel> Items { get; set; } = [];
}
