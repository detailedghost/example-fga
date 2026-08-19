namespace FgaPoc.Options;

/// <summary>
/// OpenFGA connection settings, bound from environment variables in Program.cs.
/// </summary>
public sealed class FgaOptions
{
    public required string ApiUrl { get; init; }
    public required string StoreName { get; init; }

    /// <summary>Null selects the unauthenticated local server; set only for hosted Okta FGA.</summary>
    public FgaClientCredentials? Credentials { get; init; }
}

/// <summary>OAuth client-credentials settings that hosted Okta FGA requires.</summary>
public sealed class FgaClientCredentials
{
    public required string ApiTokenIssuer { get; init; }
    public required string ApiAudience { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
