namespace GrpcSample.Domain.Configuration;

public sealed class ApiKeyAuthOptions
{
    public IReadOnlyDictionary<string, ApiKeyIdentity> Keys { get; set; } =
        Auth.ApiKeyConstants.Keys.ToDictionary(
            kvp => kvp.Key,
            kvp => new ApiKeyIdentity(kvp.Value.UserName, kvp.Value.Role),
            StringComparer.Ordinal);
}
