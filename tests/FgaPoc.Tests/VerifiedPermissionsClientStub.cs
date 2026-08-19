using Amazon.VerifiedPermissions.Model;
using FgaPoc.VerifiedPermissions;

namespace FgaPoc.Tests;

public sealed class VerifiedPermissionsClientStub : IVerifiedPermissionsClient
{
    public Func<GetPolicyStoreRequest, GetPolicyStoreResponse> GetPolicyStore { get; set; } =
        _ => new GetPolicyStoreResponse();
    public Func<IsAuthorizedRequest, IsAuthorizedResponse> IsAuthorized { get; set; } =
        _ => new IsAuthorizedResponse();
    public Func<ListPoliciesRequest, ListPoliciesResponse> ListPolicies { get; set; } =
        _ => new ListPoliciesResponse { Policies = [] };
    public Func<CreatePolicyRequest, CreatePolicyResponse> CreatePolicy { get; set; } =
        _ => new CreatePolicyResponse();
    public Func<DeletePolicyRequest, DeletePolicyResponse> DeletePolicy { get; set; } =
        _ => new DeletePolicyResponse();

    public List<IsAuthorizedRequest> AuthorizationRequests { get; } = [];
    public List<CreatePolicyRequest> CreatePolicyRequests { get; } = [];
    public List<DeletePolicyRequest> DeletePolicyRequests { get; } = [];

    public Task<GetPolicyStoreResponse> GetPolicyStoreAsync(
        GetPolicyStoreRequest request,
        CancellationToken ct
    ) => Task.FromResult(GetPolicyStore(request));

    public Task<IsAuthorizedResponse> IsAuthorizedAsync(
        IsAuthorizedRequest request,
        CancellationToken ct
    )
    {
        AuthorizationRequests.Add(request);
        return Task.FromResult(IsAuthorized(request));
    }

    public Task<ListPoliciesResponse> ListPoliciesAsync(
        ListPoliciesRequest request,
        CancellationToken ct
    ) => Task.FromResult(ListPolicies(request));

    public Task<CreatePolicyResponse> CreatePolicyAsync(
        CreatePolicyRequest request,
        CancellationToken ct
    )
    {
        CreatePolicyRequests.Add(request);
        return Task.FromResult(CreatePolicy(request));
    }

    public Task<DeletePolicyResponse> DeletePolicyAsync(
        DeletePolicyRequest request,
        CancellationToken ct
    )
    {
        DeletePolicyRequests.Add(request);
        return Task.FromResult(DeletePolicy(request));
    }
}
