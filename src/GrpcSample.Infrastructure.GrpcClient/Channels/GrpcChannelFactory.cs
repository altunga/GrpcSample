using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;

namespace GrpcSample.Infrastructure.GrpcClient.Channels;

public static class GrpcChannelFactory
{
    public static GrpcChannel CreateResilientChannel(string address, int maxAttempts = 4)
    {
        var retryPolicy = new MethodConfig
        {
            Names = { MethodName.Default },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = maxAttempts,
                InitialBackoff = TimeSpan.FromMilliseconds(100),
                MaxBackoff = TimeSpan.FromSeconds(2),
                BackoffMultiplier = 1.5,
                RetryableStatusCodes = { StatusCode.Unavailable }
            }
        };

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            ServiceConfig = new ServiceConfig { MethodConfigs = { retryPolicy } }
        });
    }
}
