using System.Security.Claims;
using System.Text.Encodings.Web;
using GrpcSample.Domain.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrpcSample.Infrastructure.Grpc.Auth;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyValidator apiKeyValidator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyConstants.ApiKeyHeader, out var values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedKey = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!apiKeyValidator.TryValidate(providedKey, out var identityInfo))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, identityInfo.UserName),
            new Claim(ClaimTypes.Role, identityInfo.Role)
        };
        var identity = new ClaimsIdentity(claims, ApiKeyConstants.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, ApiKeyConstants.AuthenticationScheme)));
    }
}
