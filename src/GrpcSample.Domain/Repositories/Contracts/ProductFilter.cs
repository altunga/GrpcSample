using GrpcSample.Domain.Models.Products;

namespace GrpcSample.Domain.Repositories.Contracts;

public sealed record ProductFilter(
    ProductCategory? Category = null,
    string? SearchTerm = null,
    bool ActiveOnly = false,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 20);
