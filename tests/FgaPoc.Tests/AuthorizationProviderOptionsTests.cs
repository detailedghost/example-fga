using FgaPoc.Options;
using Microsoft.Extensions.Configuration;

namespace FgaPoc.Tests;

public sealed class AuthorizationProviderOptionsTests
{
    [Fact]
    public void FromConfiguration_MissingValueDefaultsToOpenFga()
    {
        var configuration = new ConfigurationBuilder().Build();

        var options = AuthorizationProviderOptions.FromConfiguration(configuration);

        Assert.Equal(AuthorizationProviders.OpenFga, options.Provider);
    }

    [Fact]
    public void FromConfiguration_NormalizesCaseAndAcceptsOktaFga()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AUTHORIZATION_PROVIDER"] = " OktaFGA " }
            )
            .Build();

        var options = AuthorizationProviderOptions.FromConfiguration(configuration);

        Assert.Equal(AuthorizationProviders.OktaFga, options.Provider);
    }

    [Fact]
    public void FromConfiguration_UnknownValueThrows()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AUTHORIZATION_PROVIDER"] = "unknown" }
            )
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            AuthorizationProviderOptions.FromConfiguration(configuration)
        );
    }
}
