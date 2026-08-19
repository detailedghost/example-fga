using System.Security.Cryptography;
using System.Text;
using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Options;

namespace FgaPoc.VerifiedPermissions;

public sealed class VerifiedPermissionsService(
    IVerifiedPermissionsClient client,
    VerifiedPermissionsOptions options,
    ILogger<VerifiedPermissionsService> logger
) : IPermissionService, IPermissionProviderInitializer
{
    private const string Namespace = "Trailhead";
    private const string ActionType = $"{Namespace}::Action";
    private const string UserType = $"{Namespace}::User";
    private const string BlogType = $"{Namespace}::Blog";
    private const string PostType = $"{Namespace}::Post";
    private const string BlogId = "main";
    private const int ConsistencyAttempts = 10;

    public string ProviderId => AuthorizationProviders.VerifiedPermissions;
    public string ProviderDisplayName => "Amazon Verified Permissions";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var store = await client.GetPolicyStoreAsync(
            new GetPolicyStoreRequest { PolicyStoreId = options.PolicyStoreId },
            ct
        );
        logger.LogInformation(
            "Resolved Amazon Verified Permissions policy store {PolicyStoreId} ({Arn})",
            options.PolicyStoreId,
            store.Arn
        );
    }

    public Task<bool> CanCreatePostAsync(string username, CancellationToken ct = default) =>
        IsAuthorizedAsync(username, "createPost", Blog(), null, ct);

    public Task<bool> CanManageAccessAsync(string username, CancellationToken ct = default) =>
        IsAuthorizedAsync(username, "manageAccess", Blog(), null, ct);

    public Task<bool> CanReadPostAsync(
        string username,
        Post post,
        CancellationToken ct = default
    ) => IsAuthorizedAsync(username, "readPost", PostEntity(post.Id), post, ct);

    public Task<bool> CanEditPostAsync(
        string username,
        Post post,
        CancellationToken ct = default
    ) => IsAuthorizedAsync(username, "editPost", PostEntity(post.Id), post, ct);

    public Task<bool> CanDeletePostAsync(
        string username,
        Post post,
        CancellationToken ct = default
    ) => IsAuthorizedAsync(username, "deletePost", PostEntity(post.Id), post, ct);

    public async Task<string?> GetHighestRoleAsync(string username, CancellationToken ct = default)
    {
        foreach (var role in BlogAuthorizationModel.Roles)
            if ((await ListRolePoliciesAsync(role, username, ct)).Count > 0)
                return role;
        return null;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        string username,
        CancellationToken ct = default
    ) => BlogAuthorizationModel.PermissionsForRole(await GetHighestRoleAsync(username, ct));

    public async Task<IReadOnlyList<RoleAssignment>> ReadRoleAssignmentsAsync(
        CancellationToken ct = default
    )
    {
        var assignments = new List<RoleAssignment>();
        foreach (var role in BlogAuthorizationModel.Roles)
        {
            var policies = await ListRolePoliciesAsync(role, null, ct);
            assignments.AddRange(
                policies
                    .Where(policy => policy.Principal?.EntityType == UserType)
                    .Select(policy => new RoleAssignment(policy.Principal.EntityId, role))
            );
        }
        return assignments.Distinct().ToList();
    }

    public async Task GrantRoleAsync(string username, string role, CancellationToken ct = default)
    {
        BlogAuthorizationModel.EnsureKnownRole(role);
        if ((await ListRolePoliciesAsync(role, username, ct)).Count > 0)
            return;

        try
        {
            await client.CreatePolicyAsync(
                new CreatePolicyRequest
                {
                    PolicyStoreId = options.PolicyStoreId,
                    Name = PolicyName(username, role),
                    ClientToken = PolicyToken(username, role),
                    Definition = new PolicyDefinition
                    {
                        TemplateLinked = new TemplateLinkedPolicyDefinition
                        {
                            PolicyTemplateId = TemplateName(role),
                            Principal = User(username),
                            Resource = Blog(),
                        },
                    },
                },
                ct
            );
        }
        catch (ConflictException)
        {
            // A concurrent or retried grant already reached the desired state.
        }

        await WaitForRoleStateAsync(username, role, expected: true, ct);
    }

    public async Task RevokeRoleAsync(string username, string role, CancellationToken ct = default)
    {
        BlogAuthorizationModel.EnsureKnownRole(role);
        var policies = await ListRolePoliciesAsync(role, username, ct);
        foreach (var policy in policies)
        {
            try
            {
                await client.DeletePolicyAsync(
                    new DeletePolicyRequest
                    {
                        PolicyStoreId = options.PolicyStoreId,
                        PolicyId = policy.PolicyId,
                    },
                    ct
                );
            }
            catch (ResourceNotFoundException)
            {
                // A concurrent or retried revoke already reached the desired state.
            }
        }

        if (policies.Count > 0)
            await WaitForRoleStateAsync(username, role, expected: false, ct);
    }

    // AVP receives the post's parent and owner from PostgreSQL with each authorization request.
    public Task LinkNewPostAsync(
        int postId,
        string ownerUsername,
        CancellationToken ct = default
    ) => Task.CompletedTask;

    public Task UnlinkPostAsync(int postId, string ownerUsername, CancellationToken ct = default) =>
        Task.CompletedTask;

    private async Task<bool> IsAuthorizedAsync(
        string username,
        string action,
        EntityIdentifier resource,
        Post? post,
        CancellationToken ct
    )
    {
        var request = new IsAuthorizedRequest
        {
            PolicyStoreId = options.PolicyStoreId,
            Principal = User(username),
            Action = new ActionIdentifier { ActionType = ActionType, ActionId = action },
            Resource = resource,
        };

        if (post is not null)
        {
            request.Entities = new EntitiesDefinition
            {
                EntityList =
                [
                    new EntityItem
                    {
                        Identifier = resource,
                        Parents = [Blog()],
                        Attributes = new Dictionary<string, AttributeValue>
                        {
                            ["owner"] = new() { EntityIdentifier = User(post.AuthorUsername) },
                        },
                    },
                ],
            };
        }

        var response = await client.IsAuthorizedAsync(request, ct);
        if (response.Errors is { Count: > 0 })
            logger.LogWarning(
                "Amazon Verified Permissions returned evaluation errors: {Errors}",
                string.Join("; ", response.Errors.Select(error => error.ErrorDescription))
            );
        return response.Decision == Decision.ALLOW;
    }

    private async Task<IReadOnlyList<PolicyItem>> ListRolePoliciesAsync(
        string role,
        string? username,
        CancellationToken ct
    )
    {
        var policies = new List<PolicyItem>();
        string? nextToken = null;
        do
        {
            var filter = new PolicyFilter
            {
                PolicyType = PolicyType.TEMPLATE_LINKED,
                PolicyTemplateId = TemplateName(role),
                Resource = new EntityReference { Identifier = Blog() },
            };
            if (username is not null)
                filter.Principal = new EntityReference { Identifier = User(username) };

            var response = await client.ListPoliciesAsync(
                new ListPoliciesRequest
                {
                    PolicyStoreId = options.PolicyStoreId,
                    Filter = filter,
                    MaxResults = 50,
                    NextToken = nextToken,
                },
                ct
            );
            if (response.Policies is not null)
                policies.AddRange(response.Policies);
            nextToken = response.NextToken;
        } while (!string.IsNullOrEmpty(nextToken));

        return policies;
    }

    private async Task WaitForRoleStateAsync(
        string username,
        string role,
        bool expected,
        CancellationToken ct
    )
    {
        for (var attempt = 1; attempt <= ConsistencyAttempts; attempt++)
        {
            var exists = (await ListRolePoliciesAsync(role, username, ct)).Count > 0;
            if (exists == expected)
                return;
            if (attempt < ConsistencyAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }

        throw new InvalidOperationException(
            $"Amazon Verified Permissions did not converge after updating {role} for {username}."
        );
    }

    private static string TemplateName(string role) => $"name/blog-role-{role}";

    private static string PolicyName(string username, string role) =>
        $"name/role-grant-{PolicyHash(username, role)}";

    private static string PolicyToken(string username, string role) => PolicyHash(username, role);

    private static string PolicyHash(string username, string role) =>
        Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{username}\0{role}")))
            .ToLowerInvariant();

    private static EntityIdentifier User(string username) =>
        new() { EntityType = UserType, EntityId = username };

    private static EntityIdentifier Blog() => new() { EntityType = BlogType, EntityId = BlogId };

    private static EntityIdentifier PostEntity(int id) =>
        new() { EntityType = PostType, EntityId = id.ToString() };
}
