using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Abstractions;
using GrpcSample.Infrastructure.Grpc.Configuration;
using GrpcSample.Infrastructure.Grpc.Errors;

namespace GrpcSample.Infrastructure.Grpc.Interceptors;

public sealed class ServerAuditInterceptor(
    IAuditService auditService,
    IGrpcExceptionMapper exceptionMapper,
    GrpcEngineOptions engineOptions) : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation) =>
        ExecuteObservedAsync(context, () => continuation(request, context));

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation) =>
        ExecuteObservedAsync(context, async () =>
        {
            await continuation(request, responseStream, context);
            return true;
        });

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation) =>
        ExecuteObservedAsync(context, () => continuation(requestStream, context));

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation) =>
        ExecuteObservedAsync(context, async () =>
        {
            await continuation(requestStream, responseStream, context);
            return true;
        });

    private async Task<T> ExecuteObservedAsync<T>(ServerCallContext context, Func<Task<T>> action)
    {
        var correlationId = await PrepareHeadersAsync(context);
        var sw = Stopwatch.StartNew();
        var status = "OK";

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            status = exceptionMapper.Map(ex).AuditStatus;
            throw exceptionMapper.ToRpcException(ex);
        }
        finally
        {
            sw.Stop();
            RecordAndAppendTrailers(context, correlationId, sw.ElapsedMilliseconds, status);
        }
    }

    private async Task<string> PrepareHeadersAsync(ServerCallContext context)
    {
        var correlationId = context.RequestHeaders.GetValue(ApiKeyConstants.CorrelationIdHeader);
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        var headers = new Metadata
        {
            { ApiKeyConstants.ServerEngineHeader, engineOptions.EngineName },
            { ApiKeyConstants.CorrelationIdHeader, correlationId }
        };
        await context.WriteResponseHeadersAsync(headers);
        return correlationId;
    }

    private void RecordAndAppendTrailers(ServerCallContext context, string correlationId, long elapsedMs, string status)
    {
        var user = context.GetHttpContext().User.Identity?.Name;
        auditService.Record(engineOptions.EngineName, context.Method, correlationId, user, elapsedMs, status);

        if (!context.CancellationToken.IsCancellationRequested)
        {
            context.ResponseTrailers.Add(ApiKeyConstants.ProcessingTimeTrailer, elapsedMs.ToString());
            context.ResponseTrailers.Add(ApiKeyConstants.RequestStatusTrailer, status);
        }
    }
}
