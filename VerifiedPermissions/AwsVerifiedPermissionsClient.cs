using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;

namespace FgaPoc.VerifiedPermissions;

public sealed class AwsVerifiedPermissionsClient(IAmazonVerifiedPermissions client)
    : IVerifiedPermissionsClient
{
    public Task<GetPolicyStoreResponse> GetPolicyStoreAsync(
        GetPolicyStoreRequest request,
        CancellationToken ct
    ) => client.GetPolicyStoreAsync(request, ct);

    public Task<IsAuthorizedResponse> IsAuthorizedAsync(
        IsAuthorizedRequest request,
        CancellationToken ct
    ) => client.IsAuthorizedAsync(request, ct);

    public Task<ListPoliciesResponse> ListPoliciesAsync(
        ListPoliciesRequest request,
        CancellationToken ct
    ) => client.ListPoliciesAsync(request, ct);

    public Task<CreatePolicyResponse> CreatePolicyAsync(
        CreatePolicyRequest request,
        CancellationToken ct
    ) => client.CreatePolicyAsync(request, ct);

    public Task<DeletePolicyResponse> DeletePolicyAsync(
        DeletePolicyRequest request,
        CancellationToken ct
    ) => client.DeletePolicyAsync(request, ct);
}
