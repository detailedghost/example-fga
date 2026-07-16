using FgaPoc.Options;
using OpenFga.Sdk.Client;

namespace FgaPoc.Fga;

public static class FgaServiceCollectionExtensions
{
    /// <summary>Registers the OpenFGA client, the permission service, and the startup store resolver.</summary>
    public static IServiceCollection AddFga(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<FgaOptions>();
            return new OpenFgaClient(new ClientConfiguration { ApiUrl = options.ApiUrl });
        });
        services.AddSingleton<FgaService>();
        services.AddSingleton<FgaStoreResolver>();
        return services;
    }
}
