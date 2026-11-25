using System.Net;
using Grpc.Net.Client;
using GrpcSample.MagicOnion.Client.Filters;
using GrpcSample.MagicOnion.Server;
using GrpcSample.MagicOnion.Shared;
using GrpcSample.MagicOnion.Shared.Services;
using MagicOnion.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GrpcSample.MagicOnion.Tests;

public sealed class MagicOnionServerFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public Uri GrpcUri { get; private set; } = default!;
    public GrpcChannel Channel { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _app = MagicOnionServerHost.BuildApp([], builder =>
        {
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http2);
            });
        });

        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        GrpcUri = new Uri(address);
        Channel = GrpcChannel.ForAddress(GrpcUri);
    }

    public ICommerceMagicService CreateClient(string? apiKey = null) =>
        MagicOnionClient.Create<ICommerceMagicService>(Channel, [new ClientCorrelationFilter(apiKey)])
            .WithHeaders(new Grpc.Core.Metadata());

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
