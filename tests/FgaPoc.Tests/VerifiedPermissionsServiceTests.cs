using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Options;
using FgaPoc.VerifiedPermissions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FgaPoc.Tests;

public sealed class VerifiedPermissionsServiceTests
{
    [Fact]
    public async Task CanEditPostAsync_SendsPostOwnerAndBlogParent()
    {
        var client = new VerifiedPermissionsClientStub
        {
            IsAuthorized = _ => new IsAuthorizedResponse { Decision = Decision.ALLOW, Errors = [] },
        };
        var service = CreateService(client);
        var post = Post(id: 42, owner: "carol");

        var allowed = await service.CanEditPostAsync("carol", post);

        Assert.True(allowed);
        var request = Assert.Single(client.AuthorizationRequests);
        Assert.Equal("Trailhead::User", request.Principal.EntityType);
        Assert.Equal("carol", request.Principal.EntityId);
        Assert.Equal("editPost", request.Action.ActionId);
        Assert.Equal("42", request.Resource.EntityId);
        var entity = Assert.Single(request.Entities.EntityList);
        Assert.Equal("carol", entity.Attributes["owner"].EntityIdentifier.EntityId);
        Assert.Equal("main", Assert.Single(entity.Parents).EntityId);
    }

    [Fact]
    public async Task CanManageAccessAsync_DenyReturnsFalse()
    {
        var client = new VerifiedPermissionsClientStub
        {
            IsAuthorized = _ => new IsAuthorizedResponse { Decision = Decision.DENY, Errors = [] },
        };

        Assert.False(await CreateService(client).CanManageAccessAsync("dave"));
    }

    [Fact]
    public async Task GetHighestRoleAsync_ReturnsFirstEffectiveRole()
    {
        var client = new VerifiedPermissionsClientStub
        {
            ListPolicies = request =>
                request.Filter.PolicyTemplateId.EndsWith("writer", StringComparison.Ordinal)
                    ? new ListPoliciesResponse
                    {
                        Policies =
                        [
                            new PolicyItem
                            {
                                PolicyId = "writer-policy",
                                Principal = new EntityIdentifier
                                {
                                    EntityType = "Trailhead::User",
                                    EntityId = "carol",
                                },
                            },
                        ],
                    }
                    : new ListPoliciesResponse { Policies = [] },
        };

        Assert.Equal("writer", await CreateService(client).GetHighestRoleAsync("carol"));
    }

    [Fact]
    public async Task ReadRoleAssignmentsAsync_FollowsPagination()
    {
        var client = new VerifiedPermissionsClientStub
        {
            ListPolicies = request =>
            {
                if (!request.Filter.PolicyTemplateId.EndsWith("admin", StringComparison.Ordinal))
                    return new ListPoliciesResponse { Policies = [] };

                return request.NextToken is null
                    ? new ListPoliciesResponse
                    {
                        NextToken = "page-2",
                        Policies = [Policy("alice")],
                    }
                    : new ListPoliciesResponse { Policies = [Policy("zoe")] };
            },
        };

        var assignments = await CreateService(client).ReadRoleAssignmentsAsync();

        Assert.Contains(new RoleAssignment("alice", "admin"), assignments);
        Assert.Contains(new RoleAssignment("zoe", "admin"), assignments);
    }

    [Fact]
    public async Task GrantRoleAsync_CreatesTemplateLinkedPolicy()
    {
        var visible = false;
        var client = new VerifiedPermissionsClientStub
        {
            ListPolicies = _ => new ListPoliciesResponse
            {
                Policies = visible ? [Policy("dave")] : [],
            },
            CreatePolicy = _ =>
            {
                visible = true;
                return new CreatePolicyResponse();
            },
        };

        await CreateService(client).GrantRoleAsync("dave", "reader");

        var request = Assert.Single(client.CreatePolicyRequests);
        Assert.Equal("name/blog-role-reader", request.Definition.TemplateLinked.PolicyTemplateId);
        Assert.Equal("dave", request.Definition.TemplateLinked.Principal.EntityId);
        Assert.Equal("main", request.Definition.TemplateLinked.Resource.EntityId);
        Assert.StartsWith("name/role-grant-", request.Name, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GrantRoleAsync_ExistingPolicyDoesNotCreateDuplicate()
    {
        var client = new VerifiedPermissionsClientStub
        {
            ListPolicies = _ => new ListPoliciesResponse { Policies = [Policy("dave")] },
        };

        await CreateService(client).GrantRoleAsync("dave", "reader");

        Assert.Empty(client.CreatePolicyRequests);
    }

    private static VerifiedPermissionsService CreateService(VerifiedPermissionsClientStub client) =>
        new(
            client,
            new VerifiedPermissionsOptions
            {
                Region = "us-east-1",
                PolicyStoreId = "policy-store-alias/test",
            },
            NullLogger<VerifiedPermissionsService>.Instance
        );

    private static PolicyItem Policy(string username) =>
        new()
        {
            PolicyId = $"policy-{username}",
            Principal = new EntityIdentifier
            {
                EntityType = "Trailhead::User",
                EntityId = username,
            },
        };

    private static Post Post(int id, string owner) =>
        new()
        {
            Id = id,
            Title = "Test post",
            Body = "Body",
            AuthorUsername = owner,
            CreatedAt = DateTimeOffset.UnixEpoch,
        };
}
