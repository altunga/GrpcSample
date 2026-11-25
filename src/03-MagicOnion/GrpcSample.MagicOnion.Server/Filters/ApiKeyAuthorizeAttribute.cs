using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;

namespace GrpcSample.MagicOnion.Server.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiKeyAuthorizeAttribute(string? requiredRole = null) : MagicOnionFilterAttribute
{
    public override ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        var user = context.CallContext.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
            throw new ReturnStatusException(StatusCode.Unauthenticated, "Valid x-api-key header is required.");

        if (!string.IsNullOrWhiteSpace(requiredRole) && !user.IsInRole(requiredRole))
            throw new ReturnStatusException(StatusCode.PermissionDenied, $"Role '{requiredRole}' is required.");

        return next(context);
    }
}
