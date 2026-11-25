using Grpc.Core;
using GrpcSample.Domain.Exceptions;

namespace GrpcSample.Infrastructure.Grpc.Errors;

public sealed class GrpcExceptionMapper : IGrpcExceptionMapper
{
    public GrpcMappedError Map(Exception exception) => exception switch
    {
        RpcException rpc => new GrpcMappedError(rpc.StatusCode, rpc.Status.Detail, rpc.StatusCode.ToString()),
        DomainNotFoundException e => new GrpcMappedError(StatusCode.NotFound, e.Message, "NotFound"),
        DomainAlreadyExistsException e => new GrpcMappedError(StatusCode.AlreadyExists, e.Message, "AlreadyExists"),
        DomainValidationException e => new GrpcMappedError(StatusCode.InvalidArgument, e.Message, "InvalidArgument"),
        DomainPreconditionFailedException e => new GrpcMappedError(StatusCode.FailedPrecondition, e.Message, "FailedPrecondition"),
        OperationCanceledException => new GrpcMappedError(StatusCode.Cancelled, "Request was cancelled by client or deadline.", "Cancelled"),
        _ => new GrpcMappedError(StatusCode.Internal, exception.Message, "Internal")
    };

    public RpcException ToRpcException(Exception exception)
    {
        if (exception is RpcException rpc)
            return rpc;

        var mapped = Map(exception);
        return new RpcException(new Status(mapped.StatusCode, mapped.Message));
    }
}
