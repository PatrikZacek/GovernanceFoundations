using Governance.Abstractions.Action;

namespace Governance.Abstractions.Contracts;

public sealed record GovernanceResult(
    ReasonCode Code,
    string CorrelationId,
    ActionView? Action = null)
{
    public Guid? ActionId => Action?.ActionId;
}

