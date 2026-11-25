namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record CreateOrderInput(
    string CustomerId,
    string ShippingAddress,
    IReadOnlyList<CreateOrderItemInput> Items);
