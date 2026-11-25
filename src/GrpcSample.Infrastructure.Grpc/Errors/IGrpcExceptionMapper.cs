using Grpc.Core;

namespace GrpcSample.Infrastructure.Grpc.Errors;

public interface IGrpcExceptionMapper
{
    GrpcMappedError Map(Exception exception);
    RpcException ToRpcException(Exception exception);
}
