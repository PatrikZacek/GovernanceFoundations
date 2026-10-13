using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;
using Governance.Core.Runtime;

namespace Governance.Core;

// Shared domain-command semantics; adapters must commit returned snapshot and event atomically.
public static class GovernanceTransitions
{
    public static bool BeforeClaim(ActionState s)
    {
        return s is ActionState.Submitted or ActionState.Ready or ActionState.AwaitingApproval
            or ActionState.EvaluationFailed;
    }

    public static StoreResult Apply(GovernedAction governedAction, StoreCommand storeCommand, DateTimeOffset now)
    {
        if (governedAction.TenantId != storeCommand.TenantId || governedAction.ActionId != storeCommand.ActionId)
        {
            return new StoreResult(StoreResultCode.NotFoundOrUnauthorized, null);
        }

        if (storeCommand.Kind == CommandKind.DecideApproval && governedAction.ApprovalDecision is { } old && storeCommand.Decision is { } replay &&
            old.RequestId == replay.RequestId
            && old.ApproverId == replay.ApproverId
            && old.Approved == replay.Approved)
        {
            return new StoreResult(StoreResultCode.Ok, governedAction);
        }

        if (governedAction.Revision != storeCommand.ExpectedRevision)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        var next = governedAction;
        var storeResult = GetStoreResult(storeCommand, governedAction, now, ref next);
        return storeResult ?? new StoreResult(StoreResultCode.Ok, next with
        {
            Revision = governedAction.Revision + 1,
            Code = storeCommand.Code
        });
    }

    private static StoreResult? GetStoreResult(StoreCommand storeCommand, GovernedAction governedAction,DateTimeOffset now , ref GovernedAction next)
    {
        return storeCommand.Kind switch
        {
            CommandKind.Evaluate => ExecuteEvaluateCommand(governedAction, storeCommand, ref next),
            CommandKind.DecideApproval => ExecuteDecideApprovalCommand(governedAction, storeCommand, now, ref next),
            CommandKind.Claim => ExecuteClaimCommand(governedAction, storeCommand, now, ref next),
            CommandKind.RecordOutcome => ExecuteRecordOutcomeCommand(governedAction, storeCommand, ref next),
            CommandKind.RecordVerification => ExecuteRecordVerificationCommand(governedAction, storeCommand, ref next),
            CommandKind.Cancel => ExecuteCancelCommand(governedAction, ref next),
            CommandKind.RetryEvaluation => ExecuteRetryEvaluationCommand(governedAction, ref next),
            CommandKind.RecoverClaim => ExecuteRecoverClaimCommand(governedAction, ref next),
            _ => new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction)
        };
    }

    private static StoreResult? ExecuteRecoverClaimCommand(GovernedAction governedAction, ref GovernedAction next)
    {
        if (governedAction.State != ActionState.Executing)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        next = governedAction with
        {
            State = ActionState.OutcomeUnknown
        };
        return null;
    }

    private static StoreResult? ExecuteRetryEvaluationCommand(GovernedAction governedAction, ref GovernedAction next)
    {
        if (governedAction.State != ActionState.EvaluationFailed)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        next = governedAction with
        {
            State = ActionState.Submitted
        };
        return null;
    }

    private static StoreResult? ExecuteCancelCommand(GovernedAction governedAction, ref GovernedAction next)
    {
        if (!BeforeClaim(governedAction.State))
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        next = governedAction with
        {
            State = ActionState.Cancelled
        };
        return null;
    }

    private static StoreResult? ExecuteRecordVerificationCommand(GovernedAction governedAction,
        StoreCommand storeCommand,
        ref GovernedAction next)
    {
        if (governedAction.State != ActionState.Verifying || storeCommand.Verification is null ||
            storeCommand.Verification.AttemptId != governedAction.Attempt?.Id)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        next = governedAction with
        {
            State = storeCommand.Verification.Result.Outcome switch
            {
                VerificationOutcome.Verified => ActionState.Succeeded,
                VerificationOutcome.Mismatch => ActionState.VerificationFailed,
                _ => ActionState.OutcomeUnknown
            },
            Verification = storeCommand.Verification
        };
        return null;
    }

    private static StoreResult? ExecuteRecordOutcomeCommand(GovernedAction governedAction, StoreCommand storeCommand,
        ref GovernedAction next)
    {
        if (governedAction.State is not (ActionState.Executing or ActionState.OutcomeUnknown)
            || governedAction.Attempt is null
            || storeCommand.Outcome is null)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        var state = storeCommand.Outcome.Effect switch
        {
            Effect.Applied => ActionState.Verifying,
            Effect.NotApplied => storeCommand.Outcome.Code == RuntimeReasonCode.TargetVersionConflict &&
                                 governedAction.State == ActionState.Executing
                ? ActionState.Superseded
                : ActionState.Failed,
            _ => ActionState.OutcomeUnknown
        };
        next = governedAction with
        {
            State = state,
            Attempt = governedAction.Attempt with
            {
                Result = storeCommand.Outcome
            }
        };
        return null;
    }

    private static StoreResult? ExecuteClaimCommand(GovernedAction governedAction, StoreCommand storeCommand, DateTimeOffset now,
        ref GovernedAction next)
    {
        if (governedAction.State != ActionState.Ready
            || storeCommand.Attempt is null
            || governedAction.Attempt is not null
            || governedAction.LastEvaluation?.Id != storeCommand.Attempt.EvaluationId
            || governedAction.LastEvaluation.Phase != EvaluationPhase.PreExecution)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        if (now >= governedAction.ExpiresAt || (governedAction.Approval is not null && now >= governedAction.Approval.Deadline))
        {
            return new StoreResult(StoreResultCode.ProposalExpired, governedAction);
        }

        switch (governedAction.LastEvaluation.Decision)
        {
            case PolicyDecision.RequireApproval when
                (governedAction.ApprovalDecision?.Approved != true || governedAction.Approval is null):
                return new StoreResult(StoreResultCode.ApprovalRequired, governedAction);
            case PolicyDecision.Deny or PolicyDecision.Unavailable:
                return new StoreResult(StoreResultCode.PolicyDenied, governedAction);
            default:
                next = governedAction with
                {
                    State = ActionState.Executing,
                    Attempt = storeCommand.Attempt
                };
                return null;
        }
    }

    private static StoreResult? ExecuteDecideApprovalCommand(GovernedAction governedAction, StoreCommand storeCommand, DateTimeOffset now,
        ref GovernedAction next)
    {
        if (governedAction.State != ActionState.AwaitingApproval
            || governedAction.Approval is null
            || storeCommand.Decision is null
            || storeCommand.Decision.RequestId != governedAction.Approval.Id
            || governedAction.ApprovalDecision is not null)
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        if (now >= governedAction.ExpiresAt || now >= governedAction.Approval.Deadline)
        {
            return new StoreResult(StoreResultCode.ApprovalExpired, governedAction);
        }

        next = governedAction with
        {
            State = storeCommand.Decision.Approved ? ActionState.Ready : ActionState.Rejected,
            ApprovalDecision = storeCommand.Decision
        };
        return null;
    }

    private static StoreResult? ExecuteEvaluateCommand(GovernedAction governedAction, StoreCommand storeCommand,
        ref GovernedAction next)
    {
        if (governedAction.State is not (ActionState.Submitted or ActionState.Ready or ActionState.AwaitingApproval)
            || storeCommand.NextState is not
                (ActionState.Ready
                or ActionState.AwaitingApproval
                or ActionState.Denied
                or ActionState.EvaluationFailed
                or ActionState.Expired
                or ActionState.Superseded))
        {
            return new StoreResult(StoreResultCode.ConcurrencyConflict, governedAction);
        }

        next = governedAction with
        {
            State = storeCommand.NextState.Value,
            LastEvaluation = storeCommand.Evaluation ?? governedAction.LastEvaluation,
            Approval = storeCommand.Approval ?? governedAction.Approval
        };
        return null;
    }

    public static AuditEvent Event(GovernedAction a, AuditEventType type, string actor, string correlationId,
        DateTimeOffset now)
    {
        return new AuditEvent(Guid.NewGuid(), a.TenantId, a.ActionId, a.Revision + 1, type, now, actor, correlationId,
                a.Code)
            { SafeMetadataJson = GovernanceAuditProjection.Project(a, type) };
    }
}
