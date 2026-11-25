using GrpcSample.CodeFirst.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var app = CodeFirstServerHost.BuildApp(args, builder =>
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5201, o => o.Protocols = HttpProtocols.Http2);
        options.ListenLocalhost(5200, o => o.Protocols = HttpProtocols.Http1);
    });
});

app.Run();
