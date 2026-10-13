using Governance.Abstractions.Policy;

namespace Governance.Abstractions.Contracts;

public sealed record PreviewResult(
    ReasonCode Code,
    string CorrelationId,
    PolicyEvaluation? Evaluation,
    bool IsBinding = false);

