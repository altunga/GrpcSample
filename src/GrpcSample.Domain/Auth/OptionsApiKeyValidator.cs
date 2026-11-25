using GrpcSample.Domain.Configuration;

namespace GrpcSample.Domain.Auth;

public sealed class OptionsApiKeyValidator(ApiKeyAuthOptions options) : IApiKeyValidator
{
    public bool TryValidate(string apiKey, out ApiKeyIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            identity = default!;
            return false;
        }

        return options.Keys.TryGetValue(apiKey, out identity!);
    }
}
