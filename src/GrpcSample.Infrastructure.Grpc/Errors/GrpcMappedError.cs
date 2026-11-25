using Grpc.Core;

namespace GrpcSample.Infrastructure.Grpc.Errors;

public sealed record GrpcMappedError(StatusCode StatusCode, string Message, string AuditStatus);
