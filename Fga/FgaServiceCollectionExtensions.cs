using FgaPoc.Options;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Configuration;

namespace FgaPoc.Fga;

public static class FgaServiceCollectionExtensions
{
    /// <summary>Registers the OpenFGA client, the permission service, and the startup store resolver.</summary>
    public static IServiceCollection AddFga(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<FgaOptions>();
            return new OpenFgaClient(
                new ClientConfiguration
                {
                    ApiUrl = options.ApiUrl,
                    Credentials = CredentialsFor(options.Credentials),
                }
            );
        });
        services.AddSingleton<FgaService>();
        services.AddSingleton<FgaStoreResolver>();
        return services;
    }

    private static Credentials? CredentialsFor(FgaClientCredentials? credentials) =>
        credentials is null
            ? null
            : new Credentials
            {
                Method = CredentialsMethod.ClientCredentials,
                Config = new CredentialsConfig
                {
                    ApiTokenIssuer = credentials.ApiTokenIssuer,
                    ApiAudience = credentials.ApiAudience,
                    ClientId = credentials.ClientId,
                    ClientSecret = credentials.ClientSecret,
                },
            };
}
