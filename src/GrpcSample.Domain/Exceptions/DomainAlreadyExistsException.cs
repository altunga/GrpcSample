namespace GrpcSample.Domain.Exceptions;

public sealed class DomainAlreadyExistsException(string message) : Exception(message);
