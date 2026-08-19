using FgaPoc.Authorization;
using FgaPoc.Fga;
using FgaPoc.Options;
using FgaPoc.VerifiedPermissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FgaPoc.Tests;

public sealed class PermissionProviderServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(AuthorizationProviders.OpenFga, typeof(FgaService))]
    [InlineData(AuthorizationProviders.VerifiedPermissions, typeof(VerifiedPermissionsService))]
    [InlineData(AuthorizationProviders.OktaFga, typeof(FgaService))]
    public void AddPermissionProvider_ResolvesSelectedImplementationThroughSharedInterface(
        string providerName,
        Type expectedType
    )
    {
        var configuration = ConfigurationFor(providerName);
        var providerOptions = AuthorizationProviderOptions.FromConfiguration(configuration);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPermissionProvider(configuration, providerOptions);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType(expectedType, serviceProvider.GetRequiredService<IPermissionService>());
    }

    [Theory]
    [InlineData(AuthorizationProviders.OpenFga, AuthorizationProviders.OpenFga)]
    [InlineData(AuthorizationProviders.OktaFga, AuthorizationProviders.OktaFga)]
    public void AddPermissionProvider_SharedFgaClientStillReportsTheSelectedProvider(
        string providerName,
        string expectedProviderId
    )
    {
        var configuration = ConfigurationFor(providerName);
        var providerOptions = AuthorizationProviderOptions.FromConfiguration(configuration);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPermissionProvider(configuration, providerOptions);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Equal(
            expectedProviderId,
            serviceProvider.GetRequiredService<IPermissionService>().ProviderId
        );
    }

    [Fact]
    public void AddPermissionProvider_OktaFgaWithoutCredentialsThrows()
    {
        var values = new Dictionary<string, string?>
        {
            ["AUTHORIZATION_PROVIDER"] = AuthorizationProviders.OktaFga,
            ["OKTA_FGA_API_URL"] = "https://api.us1.fga.dev",
            ["OKTA_FGA_STORE_NAME"] = "fga-blog-poc",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var providerOptions = AuthorizationProviderOptions.FromConfiguration(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddPermissionProvider(configuration, providerOptions)
        );
    }

    private static IConfiguration ConfigurationFor(string providerName) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AUTHORIZATION_PROVIDER"] = providerName,
                    ["AWS_REGION"] = "us-east-2",
                    ["AVP_POLICY_STORE_ID"] = "policy-store-alias/test",
                    ["OKTA_FGA_API_URL"] = "https://api.us1.fga.dev",
                    ["OKTA_FGA_STORE_NAME"] = "fga-blog-poc",
                    ["OKTA_FGA_API_TOKEN_ISSUER"] = "fga.us.auth0.com",
                    ["OKTA_FGA_API_AUDIENCE"] = "https://api.us1.fga.dev/",
                    ["OKTA_FGA_CLIENT_ID"] = "test-client-id",
                    ["OKTA_FGA_CLIENT_SECRET"] = "test-client-secret",
                }
            )
            .Build();
}
