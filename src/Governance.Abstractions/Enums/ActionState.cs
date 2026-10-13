
namespace Governance.Abstractions.Enums;

public enum ActionState
{
    Submitted,
    EvaluationFailed,
    AwaitingApproval,
    Ready,
    Executing,
    Verifying,
    OutcomeUnknown,
    Succeeded,
    Denied,
    Rejected,
    Expired,
    Cancelled,
    Superseded,
    Failed,
    VerificationFailed
}

