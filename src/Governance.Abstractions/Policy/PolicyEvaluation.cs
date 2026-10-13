using System.Collections.Immutable;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Policy;

public sealed record PolicyEvaluation(
Guid Id,
string TenantId,
Guid ActionId,
EvaluationPhase Phase,
DateTimeOffset At,
string ManifestHash,
FactsSnapshot Facts,
ImmutableArray<PolicyItem> Results,
PolicyDecision Decision
);

