using FgaPoc.Options;
using OpenFga.Sdk.Client;

namespace FgaPoc.Endpoints;

/// <summary>
/// Exposes which OpenFGA store the app resolved at startup, so the local playground page can
/// read the model and tuples straight from the FGA API without hardcoding .env values in JS.
/// </summary>
public static class StoreEndpoints
{
    public static void MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/fga/store",
                (FgaOptions options, OpenFgaClient client) =>
                    Results.Json(
                        new
                        {
                            apiUrl = options.ApiUrl,
                            storeName = options.StoreName,
                            storeId = client.StoreId,
                            modelId = client.AuthorizationModelId,
                        }
                    )
            )
            .RequireAuthorization();
    }
}
