using System.Net;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using GrpcSample.CodeFirst.Contracts;
using GrpcSample.CodeFirst.Contracts.Services;
using GrpcSample.CodeFirst.Server;
using GrpcSample.Infrastructure.GrpcClient.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;
using Xunit;

namespace GrpcSample.CodeFirst.Tests;

public sealed class CodeFirstGrpcServerFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public Uri GrpcUri { get; private set; } = default!;
    public Uri HttpUri { get; private set; } = default!;
    public GrpcChannel Channel { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _app = CodeFirstServerHost.BuildApp([], builder =>
        {
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http2);
                options.Listen(IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http1);
            });
        });

        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.ToList();

        GrpcUri = new Uri(addresses[0]);
        HttpUri = new Uri(addresses[1]);
        Channel = GrpcChannel.ForAddress(GrpcUri);
    }

    public ICommerceCodeFirstService CreateClient(string? apiKey = null)
    {
        var invoker = Channel.Intercept(new ClientCorrelationInterceptor("codefirst-test-1.0", apiKey, "cf"));
        return invoker.CreateGrpcService<ICommerceCodeFirstService>();
    }

    public async Task DisposeAsync()
    {
        Channel.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
