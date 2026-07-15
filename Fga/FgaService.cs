using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace FgaPoc.Fga;

/// <summary>A blog-level role assignment read back from OpenFGA (e.g. carol → writer).</summary>
public sealed record RoleAssignment(string Username, string Role);

/// <summary>
/// Thin wrapper over <see cref="OpenFgaClient"/> expressing the Trailhead permission
/// questions in the app's own vocabulary. All authorization decisions flow through here.
/// </summary>
public sealed class FgaService(OpenFgaClient client)
{
    public const string BlogObject = "blog:main";

    // The nested blog roles, most to least privileged — used by the admin access page.
    public static readonly IReadOnlyList<string> Roles = ["admin", "editor", "writer", "reader"];

    public Task<bool> CanCreatePostAsync(string username, CancellationToken ct = default) =>
        CheckAsync(User(username), "writer", BlogObject, ct);

    public Task<bool> CanManageAccessAsync(string username, CancellationToken ct = default) =>
        CheckAsync(User(username), "admin", BlogObject, ct);

    /// <summary>Whether the user holds (or inherits) the given blog role — used to show the current role.</summary>
    public Task<bool> HasBlogRoleAsync(
        string username,
        string role,
        CancellationToken ct = default
    ) => CheckAsync(User(username), role, BlogObject, ct);

    public Task<bool> CanReadPostAsync(
        string username,
        int postId,
        CancellationToken ct = default
    ) => CheckAsync(User(username), "can_read", Post(postId), ct);

    public Task<bool> CanEditPostAsync(
        string username,
        int postId,
        CancellationToken ct = default
    ) => CheckAsync(User(username), "can_edit", Post(postId), ct);

    public Task<bool> CanDeletePostAsync(
        string username,
        int postId,
        CancellationToken ct = default
    ) => CheckAsync(User(username), "can_delete", Post(postId), ct);

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
            .Where(r => Roles.Contains(r.Role))
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
                    Object = Post(postId),
                },
                new ClientTupleKey
                {
                    User = User(ownerUsername),
                    Relation = "owner",
                    Object = Post(postId),
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
                    Object = Post(postId),
                },
                new ClientTupleKeyWithoutCondition
                {
                    User = User(ownerUsername),
                    Relation = "owner",
                    Object = Post(postId),
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
        if (!Roles.Contains(role))
            throw new ArgumentException($"Unknown role '{role}'", nameof(role));
    }

    private static string User(string username) => $"user:{username}";

    private static string Post(int id) => $"post:{id}";

    private static string StripPrefix(string qualified) =>
        qualified.Contains(':') ? qualified[(qualified.IndexOf(':') + 1)..] : qualified;
}
