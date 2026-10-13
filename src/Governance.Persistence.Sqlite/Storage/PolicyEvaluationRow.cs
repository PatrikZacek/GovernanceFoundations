using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class PolicyEvaluationRow : IActionHistoryRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public EvaluationPhase Phase { get; set; }
    public DateTimeOffset At { get; set; }
    public string ManifestHash { get; set; } = string.Empty;
    public string FactsVersion { get; set; } = string.Empty;
    public string FactsValuesJson { get; set; } = string.Empty;
    public PolicyDecision Decision { get; set; }
}
