using Governance.Abstractions.Contracts;
using Governance.Abstractions.Policy;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class PolicyItemRow
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public Guid EvaluationId { get; set; }
    public int Ordinal { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public PolicyDecision Decision { get; set; }
    public ReasonCode ReasonCode { get; set; }
    public string SafeMessage { get; set; } = string.Empty;
}
