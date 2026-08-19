using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Options;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace FgaPoc.Fga;

/// <summary>
/// Thin wrapper over <see cref="OpenFgaClient"/> expressing the Trailhead permission
/// questions in the app's own vocabulary. All authorization decisions flow through here.
/// </summary>
public sealed class FgaService(OpenFgaClient client, AuthorizationProviderOptions provider)
    : IPermissionService
{
    public const string BlogObject = "blog:main";

    // Self-hosted and Okta-hosted FGA share this client, so the selected provider names it.
    public string ProviderId => provider.Provider;
    public string ProviderDisplayName =>
        provider.Provider == AuthorizationProviders.OktaFga ? "Okta FGA" : "OpenFGA";

    public Task<bool> CanCreatePostAsync(string username, CancellationToken ct = default) =>
        CheckAsync(User(username), "writer", BlogObject, ct);

    public Task<bool> CanManageAccessAsync(string username, CancellationToken ct = default) =>
        CheckAsync(User(username), "admin", BlogObject, ct);

    public async Task<string?> GetHighestRoleAsync(string username, CancellationToken ct = default)
    {
        foreach (var role in BlogAuthorizationModel.Roles)
            if (await CheckAsync(User(username), role, BlogObject, ct))
                return role;
        return null;
    }

    /// <summary>
    /// The "action:resource" permissions the user currently holds, resolved from OpenFGA — the API
    /// returns this list and the frontend just checks membership. "edit/delete:posts" map to the
    /// editor role, since editors (and admins) act on every post regardless of ownership.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        string username,
        CancellationToken ct = default
    ) => BlogAuthorizationModel.PermissionsForRole(await GetHighestRoleAsync(username, ct));

    public Task<bool> CanReadPostAsync(
        string username,
        Post post,
        CancellationToken ct = default
    ) => CheckAsync(User(username), "can_read", PostObject(post.Id), ct);

    public Task<bool> CanEditPostAsync(
        string username,
        Post post,
        CancellationToken ct = default
    ) => CheckAsync(User(username), "can_edit", PostObject(post.Id), ct);

    public Task<bool> CanDeletePostAsync(
        string username,
        Post post,
        CancellationToken ct = default
    ) => CheckAsync(User(username), "can_delete", PostObject(post.Id), ct);

    /// <summary>Grant a blog-level role. Idempotent — an existing tuple is left as-is.</summary>
    public async Task GrantRoleAsync(string username, string role, CancellationToken ct = default)
    {
        EnsureKnownRole(role);
        await WriteAsync(
            new ClientTupleKey
            {
                User = User(username),
                Relation = role,
                Object = BlogObject,
            },
            ct
        );
    }

    /// <summary>Revoke a blog-level role. Idempotent — a missing tuple is ignored.</summary>
    public async Task RevokeRoleAsync(string username, string role, CancellationToken ct = default)
    {
        EnsureKnownRole(role);
        await DeleteAsync(
            new ClientTupleKeyWithoutCondition
            {
                User = User(username),
                Relation = role,
                Object = BlogObject,
            },
            ct
        );
    }

    /// <summary>Every blog-level role tuple, for the admin access page.</summary>
    public async Task<IReadOnlyList<RoleAssignment>> ReadRoleAssignmentsAsync(
        CancellationToken ct = default
    )
    {
        var response = await client.Read(
            new ClientReadRequest { Object = BlogObject },
            cancellationToken: ct
        );
        return response
            .Tuples.Select(t => new RoleAssignment(StripPrefix(t.Key.User), t.Key.Relation))
            .Where(r => BlogAuthorizationModel.Roles.Contains(r.Role))
            .ToList();
    }

    /// <summary>Link a freshly created post to the blog and record its owner.</summary>
    public async Task LinkNewPostAsync(
        int postId,
        string ownerUsername,
        CancellationToken ct = default
    )
    {
        await WriteAsync(
            [
                new ClientTupleKey
                {
                    User = BlogObject,
                    Relation = "blog",
                    Object = PostObject(postId),
                },
                new ClientTupleKey
                {
                    User = User(ownerUsername),
                    Relation = "owner",
                    Object = PostObject(postId),
                },
            ],
            ct
        );
    }

    /// <summary>Remove a deleted post's tuples so the store stays tidy.</summary>
    public async Task UnlinkPostAsync(
        int postId,
        string ownerUsername,
        CancellationToken ct = default
    )
    {
        await DeleteAsync(
            [
                new ClientTupleKeyWithoutCondition
                {
                    User = BlogObject,
                    Relation = "blog",
                    Object = PostObject(postId),
                },
                new ClientTupleKeyWithoutCondition
                {
                    User = User(ownerUsername),
                    Relation = "owner",
                    Object = PostObject(postId),
                },
            ],
            ct
        );
    }

    private async Task<bool> CheckAsync(
        string user,
        string relation,
        string @object,
        CancellationToken ct
    )
    {
        var response = await client.Check(
            new ClientCheckRequest
            {
                User = user,
                Relation = relation,
                Object = @object,
            },
            cancellationToken: ct
        );
        return response.Allowed ?? false;
    }

    private Task WriteAsync(ClientTupleKey write, CancellationToken ct) => WriteAsync([write], ct);

    private async Task WriteAsync(List<ClientTupleKey> writes, CancellationToken ct)
    {
        try
        {
            await client.Write(new ClientWriteRequest { Writes = writes }, cancellationToken: ct);
        }
        catch (Exception) when (writes.Count == 1)
        {
            // A single duplicate write (already-granted role) is a no-op, not an error.
        }
    }

    private async Task DeleteAsync(ClientTupleKeyWithoutCondition delete, CancellationToken ct) =>
        await DeleteAsync([delete], ct);

    private async Task DeleteAsync(
        List<ClientTupleKeyWithoutCondition> deletes,
        CancellationToken ct
    )
    {
        try
        {
            await client.Write(new ClientWriteRequest { Deletes = deletes }, cancellationToken: ct);
        }
        catch (Exception)
        {
            // Deleting a tuple that isn't there is fine — the desired end state already holds.
        }
    }

    private static void EnsureKnownRole(string role)
    {
        BlogAuthorizationModel.EnsureKnownRole(role);
    }

    private static string User(string username) => $"user:{username}";

    private static string PostObject(int id) => $"post:{id}";

    private static string StripPrefix(string qualified) =>
        qualified.Contains(':') ? qualified[(qualified.IndexOf(':') + 1)..] : qualified;
}
