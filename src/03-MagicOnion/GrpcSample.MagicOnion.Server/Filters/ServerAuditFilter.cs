using System.Diagnostics;
using Grpc.Core;
using GrpcSample.Domain.Auth;
using GrpcSample.Domain.Services;
using GrpcSample.Domain.Services.Abstractions;
using GrpcSample.Infrastructure.Grpc.Configuration;
using GrpcSample.Infrastructure.Grpc.Errors;
using MagicOnion;
using MagicOnion.Server;

namespace GrpcSample.MagicOnion.Server.Filters;

public sealed class ServerAuditFilter(
    IAuditService auditService,
    IGrpcExceptionMapper exceptionMapper,
    GrpcEngineOptions engineOptions) : MagicOnionFilterAttribute
{
    public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        var callCtx = context.CallContext;
        if (callCtx.Method.EndsWith("/Connect", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        var correlationId = callCtx.RequestHeaders.GetValue(ApiKeyConstants.CorrelationIdHeader);
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        await callCtx.WriteResponseHeadersAsync(new Metadata
        {
            { ApiKeyConstants.ServerEngineHeader, engineOptions.EngineName },
            { ApiKeyConstants.CorrelationIdHeader, correlationId }
        });

        var sw = Stopwatch.StartNew();
        var status = "OK";
        try
        {
            await next(context);
        }
        catch (ReturnStatusException ex)
        {
            status = ex.StatusCode.ToString();
            throw;
        }
        catch (Exception ex)
        {
            var mapped = exceptionMapper.Map(ex);
            status = mapped.AuditStatus;
            if (ex is RpcException)
                throw;

            throw new ReturnStatusException(mapped.StatusCode, mapped.Message);
        }
        finally
        {
            sw.Stop();
            var user = callCtx.GetHttpContext().User.Identity?.Name;
            auditService.Record(engineOptions.EngineName, callCtx.Method, correlationId, user, sw.ElapsedMilliseconds, status);

            if (!callCtx.CancellationToken.IsCancellationRequested)
            {
                callCtx.ResponseTrailers.Add(ApiKeyConstants.ProcessingTimeTrailer, sw.ElapsedMilliseconds.ToString());
                callCtx.ResponseTrailers.Add(ApiKeyConstants.RequestStatusTrailer, status);
            }
        }
    }
}
