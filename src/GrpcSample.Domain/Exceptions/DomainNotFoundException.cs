namespace GrpcSample.Domain.Exceptions;

public sealed class DomainNotFoundException(string message) : Exception(message);
