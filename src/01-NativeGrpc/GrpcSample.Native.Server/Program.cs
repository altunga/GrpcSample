using GrpcSample.Native.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var app = NativeServerHost.BuildApp(args, builder =>
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5101, o => o.Protocols = HttpProtocols.Http2);
        options.ListenLocalhost(5100, o => o.Protocols = HttpProtocols.Http1);
    });
});

app.Run();
