namespace GrpcSample.Domain.Exceptions;

public sealed class DomainValidationException(string message) : Exception(message);
