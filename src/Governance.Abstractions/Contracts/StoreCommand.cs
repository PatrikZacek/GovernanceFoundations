using Governance.Abstractions.Approval;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;

namespace Governance.Abstractions.Contracts;

public sealed record StoreCommand(
    CommandKind Kind,
    string TenantId,
    Guid ActionId,
    long ExpectedRevision,
    string ActorId,
    string CorrelationId,
    ActionState? NextState = null,
    ReasonCode Code = default,
    PolicyEvaluation? Evaluation = null,
    ApprovalRequest? Approval = null,
    ApprovalDecision? Decision = null,
    ExecutionAttempt? Attempt = null,
    ExecutionResult? Outcome = null,
    VerificationRecord? Verification = null)
{
    public ReasonCode Code
    {
        get;
        init;
    } = Code.IsSpecified ? Code : RuntimeReasonCode.Ok;
}
