using GrpcSample.Infrastructure.Grpc.Extensions;
using GrpcSample.Native.Server.Services;

namespace GrpcSample.Native.Server;

public static class NativeServerHost
{
    public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configureBuilder?.Invoke(builder);

        builder.Services
            .AddCommerceDomain()
            .AddCommerceGrpcInfrastructure("native-grpc");

        builder.Services
            .AddGrpc(options => CommercePlatformServiceCollectionExtensions.ConfigureStandardGrpcOptions(options, addAuditInterceptor: true))
            .AddJsonTranscoding();

        builder.Services.AddGrpcReflection();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrpcService<CommerceGrpcService>();
        app.MapGrpcReflectionService();
        app.MapGrpcHealthChecksService();
        app.MapGet("/", () => Results.Ok(new
        {
            Engine = "Native Proto-First gRPC (.NET 11)",
            GrpcEndpoints = "CommerceService (18 RPCs), ServerReflection, HealthChecks",
            JsonTranscoding = new[] { "/v1/products", "/v1/products/{id}", "/v1/orders/{id}", "/v1/summary" }
        }));

        return app;
    }
}
