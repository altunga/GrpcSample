using GrpcSample.Infrastructure.Grpc.Extensions;
using GrpcSample.MagicOnion.Server.Filters;
using MagicOnion.Server;

namespace GrpcSample.MagicOnion.Server;

public static class MagicOnionServerHost
{
    public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configureBuilder?.Invoke(builder);

        builder.Services
            .AddCommerceDomain()
            .AddCommerceGrpcInfrastructure("magiconion");

        builder.Services.AddGrpc(options =>
            CommercePlatformServiceCollectionExtensions.ConfigureStandardGrpcOptions(options, addAuditInterceptor: false));

        builder.Services.AddMagicOnion(options =>
        {
            options.IsReturnExceptionStackTraceInErrorDetail = true;
            options.GlobalFilters.Add<ServerAuditFilter>();
        });

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapMagicOnionService();
        app.MapGrpcHealthChecksService();
        app.MapGet("/", () => Results.Ok(new
        {
            Engine = "MagicOnion v7 (.NET 11 + MessagePack)",
            Endpoints = "ICommerceMagicService (18 RPCs), ICommerceHub (StreamingHub), HealthChecks"
        }));

        return app;
    }
}
