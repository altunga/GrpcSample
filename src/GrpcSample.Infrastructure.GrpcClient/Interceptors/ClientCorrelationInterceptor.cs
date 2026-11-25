using Grpc.Core;
using Grpc.Core.Interceptors;
using GrpcSample.Domain.Auth;

namespace GrpcSample.Infrastructure.GrpcClient.Interceptors;

public sealed class ClientCorrelationInterceptor(
    string clientVersion,
    string? apiKey = null,
    string correlationPrefix = "grpc") : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, EnrichContext(context));

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, EnrichContext(context));

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(EnrichContext(context));

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(EnrichContext(context));

    private ClientInterceptorContext<TRequest, TResponse> EnrichContext<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var headers = new Metadata();
        if (context.Options.Headers is not null)
        {
            foreach (var entry in context.Options.Headers)
                headers.Add(entry);
        }

        if (headers.Get(ApiKeyConstants.CorrelationIdHeader) is null)
            headers.Add(ApiKeyConstants.CorrelationIdHeader, $"{correlationPrefix}-{Guid.NewGuid():N}"[..16]);

        headers.Add("x-client-version", clientVersion);

        if (!string.IsNullOrWhiteSpace(apiKey) && headers.Get(ApiKeyConstants.ApiKeyHeader) is null)
            headers.Add(ApiKeyConstants.ApiKeyHeader, apiKey);

        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            context.Options.WithHeaders(headers));
    }
}
