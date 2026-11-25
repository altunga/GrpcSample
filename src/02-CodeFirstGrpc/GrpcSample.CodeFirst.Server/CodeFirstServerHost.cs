using GrpcSample.CodeFirst.Contracts;
using GrpcSample.CodeFirst.Contracts.Services;
using GrpcSample.CodeFirst.Server.Services;
using GrpcSample.Infrastructure.Grpc.Extensions;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Grpc.Server;

namespace GrpcSample.CodeFirst.Server;

public static class CodeFirstServerHost
{
    public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configureBuilder?.Invoke(builder);

        builder.Services
            .AddCommerceDomain()
            .AddCommerceGrpcInfrastructure("protobuf-net-grpc");

        builder.Services.AddCodeFirstGrpc(options =>
            CommercePlatformServiceCollectionExtensions.ConfigureStandardGrpcOptions(options, addAuditInterceptor: true));

        builder.Services.AddCodeFirstGrpcReflection();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrpcService<CommerceCodeFirstService>();
        app.MapCodeFirstGrpcReflectionService();
        app.MapGrpcHealthChecksService();

        app.MapGet("/schema.proto", () =>
        {
            var generator = new SchemaGenerator();
            var schema = generator.GetSchema<ICommerceCodeFirstService>();
            return Results.Text(schema, "text/plain; charset=utf-8");
        });

        app.MapGet("/", () => Results.Ok(new
        {
            Engine = "Code-First gRPC (.NET 11 + protobuf-net.Grpc)",
            GrpcEndpoints = "ICommerceCodeFirstService (18 RPCs), CodeFirstGrpcReflection, HealthChecks",
            ProtoSchemaExport = "/schema.proto"
        }));

        return app;
    }
}
