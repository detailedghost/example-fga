using Amazon;
using Amazon.VerifiedPermissions;
using FgaPoc.Fga;
using FgaPoc.Options;
using FgaPoc.VerifiedPermissions;

namespace FgaPoc.Authorization;

public static class PermissionProviderServiceCollectionExtensions
{
    public static IServiceCollection AddPermissionProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        AuthorizationProviderOptions provider
    )
    {
        services.AddSingleton(provider);

        if (provider.Provider is AuthorizationProviders.OpenFga or AuthorizationProviders.OktaFga)
        {
            services.AddSingleton(FgaOptionsFor(configuration, provider.Provider));
            services.AddFga();
            services.AddSingleton<IPermissionService>(sp => sp.GetRequiredService<FgaService>());
            services.AddSingleton<IPermissionProviderInitializer>(sp =>
                sp.GetRequiredService<FgaStoreResolver>()
            );
            return services;
        }

        var region = configuration["AWS_REGION"];
        var policyStoreId = configuration["AVP_POLICY_STORE_ID"];
        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(policyStoreId))
            throw new InvalidOperationException(
                "AWS_REGION and AVP_POLICY_STORE_ID are required when AUTHORIZATION_PROVIDER=verifiedpermissions."
            );

        services.AddSingleton(
            new VerifiedPermissionsOptions { Region = region, PolicyStoreId = policyStoreId }
        );
        services.AddSingleton<IAmazonVerifiedPermissions>(_ => new AmazonVerifiedPermissionsClient(
            RegionEndpoint.GetBySystemName(region)
        ));
        services.AddSingleton<IVerifiedPermissionsClient, AwsVerifiedPermissionsClient>();
        services.AddSingleton<VerifiedPermissionsService>();
        services.AddSingleton<IPermissionService>(sp =>
            sp.GetRequiredService<VerifiedPermissionsService>()
        );
        services.AddSingleton<IPermissionProviderInitializer>(sp =>
            sp.GetRequiredService<VerifiedPermissionsService>()
        );
        return services;
    }

    private static FgaOptions FgaOptionsFor(IConfiguration configuration, string provider)
    {
        if (provider == AuthorizationProviders.OpenFga)
            return new FgaOptions
            {
                ApiUrl = configuration["FGA_API_URL"] ?? "http://localhost:8080",
                StoreName = configuration["FGA_STORE_NAME"] ?? "fga-blog-poc",
            };

        // Hosted Okta FGA has no local default worth guessing, so every value is required.
        var apiUrl = configuration["OKTA_FGA_API_URL"];
        var storeName = configuration["OKTA_FGA_STORE_NAME"];
        var issuer = configuration["OKTA_FGA_API_TOKEN_ISSUER"];
        var audience = configuration["OKTA_FGA_API_AUDIENCE"];
        var clientId = configuration["OKTA_FGA_CLIENT_ID"];
        var clientSecret = configuration["OKTA_FGA_CLIENT_SECRET"];

        if (
            string.IsNullOrWhiteSpace(apiUrl)
            || string.IsNullOrWhiteSpace(storeName)
            || string.IsNullOrWhiteSpace(issuer)
            || string.IsNullOrWhiteSpace(audience)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
        )
            throw new InvalidOperationException(
                "OKTA_FGA_API_URL, OKTA_FGA_STORE_NAME, OKTA_FGA_API_TOKEN_ISSUER, OKTA_FGA_API_AUDIENCE, "
                    + "OKTA_FGA_CLIENT_ID, and OKTA_FGA_CLIENT_SECRET are required when AUTHORIZATION_PROVIDER=oktafga."
            );

        return new FgaOptions
        {
            ApiUrl = apiUrl,
            StoreName = storeName,
            Credentials = new FgaClientCredentials
            {
                ApiTokenIssuer = issuer,
                ApiAudience = audience,
                ClientId = clientId,
                ClientSecret = clientSecret,
            },
        };
    }
}
