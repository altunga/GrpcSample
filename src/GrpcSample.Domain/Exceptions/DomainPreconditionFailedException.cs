namespace GrpcSample.Domain.Exceptions;

public sealed class DomainPreconditionFailedException(string message) : Exception(message);
