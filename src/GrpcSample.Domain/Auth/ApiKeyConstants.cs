namespace GrpcSample.Domain.Auth;

public static class ApiKeyConstants
{
    public const string AuthenticationScheme = "ApiKey";
    public const string ApiKeyHeader = "x-api-key";
    public const string CorrelationIdHeader = "x-correlation-id";
    public const string ServerEngineHeader = "x-server-engine";
    public const string ProcessingTimeTrailer = "x-processing-time-ms";
    public const string RequestStatusTrailer = "x-request-status";
    public const string ItemsProcessedTrailer = "x-items-processed";

    public const string AdminKey = "demo-admin-key";
    public const string UserKey = "demo-user-key";
    public const string ReadOnlyKey = "demo-readonly-key";

    public static readonly IReadOnlyDictionary<string, (string UserName, string Role)> Keys =
        new Dictionary<string, (string UserName, string Role)>(StringComparer.Ordinal)
        {
            [AdminKey] = ("admin@grpcsample.dev", "Admin"),
            [UserKey] = ("operator@grpcsample.dev", "User"),
            [ReadOnlyKey] = ("viewer@grpcsample.dev", "ReadOnly")
        };
}
