using FgaPoc.Data;
using FgaPoc.Options;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace FgaPoc.Fga;

/// <summary>
/// Idempotently prepares the OpenFGA store on startup: finds or creates the named store,
/// writes the authorization model, and (on a fresh store) seeds role + ownership tuples.
/// Runs after <see cref="DbInitializer"/> so seeded posts exist to link.
/// </summary>
public sealed class FgaBootstrapper(
    OpenFgaClient client,
    FgaService fga,
    FgaOptions options,
    PostRepository posts,
    ILogger<FgaBootstrapper> logger
)
{
    // Seed identities → their single blog-level role (roles are nested, so one each).
    private static readonly IReadOnlyDictionary<string, string> SeedRoles = new Dictionary<
        string,
        string
    >
    {
        ["alice"] = "admin",
        ["bob"] = "editor",
        ["carol"] = "writer",
        ["dave"] = "reader",
        ["erin"] = "writer",
    };

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var stores = await client.ListStores(new ClientListStoresRequest(), cancellationToken: ct);
        var existing = stores.Stores?.FirstOrDefault(s => s.Name == options.StoreName);

        if (existing is not null)
        {
            client.StoreId = existing.Id;
            client.AuthorizationModelId = await LatestOrNewModelAsync(ct);
            logger.LogInformation(
                "Reusing OpenFGA store {StoreId} ({Name})",
                existing.Id,
                options.StoreName
            );
            return;
        }

        var created = await client.CreateStore(
            new ClientCreateStoreRequest { Name = options.StoreName },
            cancellationToken: ct
        );
        client.StoreId = created.Id;
        var model = await client.WriteAuthorizationModel(FgaModel.Build(), cancellationToken: ct);
        client.AuthorizationModelId = model.AuthorizationModelId;
        logger.LogInformation(
            "Created OpenFGA store {StoreId}, model {ModelId}",
            created.Id,
            model.AuthorizationModelId
        );

        await SeedTuplesAsync(ct);
    }

    private async Task<string> LatestOrNewModelAsync(CancellationToken ct)
    {
        var models = await client.ReadAuthorizationModels(cancellationToken: ct);
        var latest = models.AuthorizationModels?.FirstOrDefault()?.Id;
        if (latest is not null)
            return latest;

        var model = await client.WriteAuthorizationModel(FgaModel.Build(), cancellationToken: ct);
        return model.AuthorizationModelId;
    }

    private async Task SeedTuplesAsync(CancellationToken ct)
    {
        foreach (var (username, role) in SeedRoles)
            await fga.GrantRoleAsync(username, role, ct);

        foreach (var post in await posts.GetAllAsync(ct))
            await fga.LinkNewPostAsync(post.Id, post.AuthorUsername, ct);

        logger.LogInformation("Seeded {Roles} role tuples and post ownership", SeedRoles.Count);
    }
}
