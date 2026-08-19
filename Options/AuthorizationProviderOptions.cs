namespace FgaPoc.Options;

public static class AuthorizationProviders
{
    public const string OpenFga = "openfga";
    public const string VerifiedPermissions = "verifiedpermissions";

    /// <summary>Okta's hosted OpenFGA. Same wire API as <see cref="OpenFga"/>, so it reuses that client.</summary>
    public const string OktaFga = "oktafga";
}

public sealed class AuthorizationProviderOptions
{
    public required string Provider { get; init; }

    public static AuthorizationProviderOptions FromConfiguration(IConfiguration configuration)
    {
        var provider = (configuration["AUTHORIZATION_PROVIDER"] ?? AuthorizationProviders.OpenFga)
            .Trim()
            .ToLowerInvariant();

        if (
            provider
            is not (
                AuthorizationProviders.OpenFga
                or AuthorizationProviders.VerifiedPermissions
                or AuthorizationProviders.OktaFga
            )
        )
            throw new InvalidOperationException(
                $"Unknown AUTHORIZATION_PROVIDER '{provider}'. Expected 'openfga', 'verifiedpermissions', or 'oktafga'."
            );

        return new AuthorizationProviderOptions { Provider = provider };
    }
}
