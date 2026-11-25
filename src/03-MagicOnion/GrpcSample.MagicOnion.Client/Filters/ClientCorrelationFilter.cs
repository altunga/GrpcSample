using GrpcSample.Domain.Auth;
using MagicOnion.Client;

namespace GrpcSample.MagicOnion.Client.Filters;

public sealed class ClientCorrelationFilter(string? apiKey = null) : IClientFilter
{
    public ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
    {
        var headers = context.CallOptions.Headers;
        if (headers?.Get(ApiKeyConstants.CorrelationIdHeader) is null)
            context.CallOptions.Headers?.Add(ApiKeyConstants.CorrelationIdHeader, $"mo-{Guid.NewGuid():N}"[..16]);

        context.CallOptions.Headers?.Add("x-client-version", "magiconion-1.0");

        if (!string.IsNullOrWhiteSpace(apiKey) && headers?.Get(ApiKeyConstants.ApiKeyHeader) is null)
            context.CallOptions.Headers?.Add(ApiKeyConstants.ApiKeyHeader, apiKey);

        return next(context);
    }
}
