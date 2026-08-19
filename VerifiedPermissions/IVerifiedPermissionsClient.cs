using Amazon.VerifiedPermissions.Model;

namespace FgaPoc.VerifiedPermissions;

/// <summary>Narrow SDK seam so the provider can be tested without calling AWS.</summary>
public interface IVerifiedPermissionsClient
{
    Task<GetPolicyStoreResponse> GetPolicyStoreAsync(
        GetPolicyStoreRequest request,
        CancellationToken ct
    );
    Task<IsAuthorizedResponse> IsAuthorizedAsync(IsAuthorizedRequest request, CancellationToken ct);
    Task<ListPoliciesResponse> ListPoliciesAsync(ListPoliciesRequest request, CancellationToken ct);
    Task<CreatePolicyResponse> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken ct);
    Task<DeletePolicyResponse> DeletePolicyAsync(DeletePolicyRequest request, CancellationToken ct);
}
