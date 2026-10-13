using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed record AuthorizationRequest(
    TrustedPrincipal Principal,
    GovernanceOperation Operation,
    string ResourceId,
    string Capability,
    string? OwnerId = null);
