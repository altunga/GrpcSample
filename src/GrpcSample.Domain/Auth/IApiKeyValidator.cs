using GrpcSample.Domain.Configuration;

namespace GrpcSample.Domain.Auth;

public interface IApiKeyValidator
{
    bool TryValidate(string apiKey, out ApiKeyIdentity identity);
}
