namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record CreateOrderItemInput(string ProductId, int Quantity);
