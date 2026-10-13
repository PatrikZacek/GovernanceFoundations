using Governance.Abstractions.Approval;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;

namespace Governance.Abstractions.Contracts;

// Internal persistence contract: host/store ports are trusted.
// Public read DTO below excludes payload and internal evidence.
public sealed record GovernedAction(
    Guid ActionId,
    string TenantId,
    string ActionTypeId,
    int SchemaVersion,
    string PayloadJson,
    string PayloadFingerprint,
    string ProposalFingerprint,
    TrustedPrincipal Proposer,
    string TargetResourceId,
    long ExpectedTargetVersion,
    string IdempotencyKey,
    DateTimeOffset SubmittedAt,
    DateTimeOffset ExpiresAt,
    string ManifestHash,
    ActionState State = ActionState.Submitted,
    long Revision = 0,
    ReasonCode Code = default,
    ApprovalRequest? Approval = null,
    ApprovalDecision? ApprovalDecision = null,
    ExecutionAttempt? Attempt = null,
    PolicyEvaluation? LastEvaluation = null,
    VerificationRecord? Verification = null
)
{
    public ReasonCode Code
    {
        get;
        init;
    } = Code.IsSpecified ? Code : RuntimeReasonCode.Submitted;

    public string ExecutionKey => ActionId.ToString("N");
}
