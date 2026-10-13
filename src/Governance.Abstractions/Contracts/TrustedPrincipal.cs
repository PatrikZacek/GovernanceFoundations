namespace Governance.Abstractions.Contracts;

public sealed record TrustedPrincipal(
    string TenantId,
    string PrincipalId,
    bool IsHuman = true,
    string? OnBehalfOfPrincipalId = null,
    string? AgentId = null);

