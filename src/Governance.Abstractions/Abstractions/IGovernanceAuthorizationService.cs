using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IGovernanceAuthorizationService
{
    ValueTask<bool> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct);
}

