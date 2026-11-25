using GrpcSample.MagicOnion.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var app = MagicOnionServerHost.BuildApp(args, builder =>
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5301, o => o.Protocols = HttpProtocols.Http2);
        options.ListenLocalhost(5300, o => o.Protocols = HttpProtocols.Http1);
    });
});

app.Run();
