using FgaPoc.Authorization;
using FgaPoc.Options;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace FgaPoc.Fga;

/// <summary>
/// Resolves the externally-provisioned OpenFGA store by name and pins it (plus its latest
/// model) on the shared client. Read-only: the store, model, and tuples are bootstrapped by
/// docker-compose from db/fga — not here. Retries briefly in case the import is still finishing.
/// </summary>
public sealed class FgaStoreResolver(
    OpenFgaClient client,
    FgaOptions options,
    ILogger<FgaStoreResolver> logger
) : IPermissionProviderInitializer
{
    private const int MaxAttempts = 15;

    public Task InitializeAsync(CancellationToken ct = default) => ResolveAsync(ct);

    public async Task ResolveAsync(CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var stores = await client.ListStores(
                new ClientListStoresRequest(),
                cancellationToken: ct
            );
            var store = stores.Stores?.FirstOrDefault(s => s.Name == options.StoreName);

            if (store is not null)
            {
                client.StoreId = store.Id;
                var models = await client.ReadAuthorizationModels(cancellationToken: ct);
                client.AuthorizationModelId =
                    models.AuthorizationModels?.FirstOrDefault()?.Id
                    ?? throw new InvalidOperationException(
                        $"OpenFGA store '{options.StoreName}' has no authorization model."
                    );
                logger.LogInformation(
                    "Resolved OpenFGA store {StoreId} ({Name})",
                    store.Id,
                    options.StoreName
                );
                return;
            }

            if (attempt >= MaxAttempts)
                throw new InvalidOperationException(
                    $"OpenFGA store '{options.StoreName}' not found after {MaxAttempts} attempts. "
                        + "Did `docker compose up` run the fga-import step?"
                );

            logger.LogInformation(
                "Waiting for OpenFGA store {Name} (attempt {Attempt}/{Max})",
                options.StoreName,
                attempt,
                MaxAttempts
            );
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
