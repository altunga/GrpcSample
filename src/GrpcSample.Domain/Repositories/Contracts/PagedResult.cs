namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
