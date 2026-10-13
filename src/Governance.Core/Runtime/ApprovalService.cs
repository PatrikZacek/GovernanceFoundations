using Governance.Abstractions.Approval;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;

namespace Governance.Core.Runtime;

internal sealed class ApprovalService(GovernanceRuntime runtime)
{
    public Task<GovernanceResult> ApproveAsync(Guid approvalId, long expectedRevision, string reason,
        CancellationToken cancellationToken = default)
    {
        return Decide(approvalId, expectedRevision, true, reason, cancellationToken);
    }

    public Task<GovernanceResult> RejectAsync(Guid approvalId, long expectedRevision, string reason,
        CancellationToken cancellationToken = default)
    {
        return Decide(approvalId, expectedRevision, false, reason, cancellationToken);
    }

    private async Task<GovernanceResult> Decide(Guid id, long revision, bool approve, string reason,
        CancellationToken cancellationToken)
    {
        var correlationId = runtime.CreateCorrelationId();
        GovernedAction? action = null;
        try
        {
            action = await runtime.Store.FindApprovalAsync(runtime.Principal.Current.TenantId, id, cancellationToken);
            if (action is null || !runtime.Principal.Current.IsHuman
                               || !await runtime.AuthorizeAsync(
                                   runtime.Principal.Current,
                                   GovernanceOperation.Approve,
                                   action.TargetResourceId,
                                   action.Approval!.Capability,
                                   action.Proposer.PrincipalId,
                                   cancellationToken
                               ))
            {
                return new GovernanceResult(RuntimeReasonCode.NotFoundOrUnauthorized, correlationId);
            }

            if (runtime.Principal.Current.PrincipalId == action.Proposer.PrincipalId
                || runtime.Principal.Current.PrincipalId == action.Proposer.OnBehalfOfPrincipalId)
            {
                return runtime.CreateResult(action, correlationId, RuntimeReasonCode.SelfApprovalForbidden);
            }

            if (action.ManifestHash != runtime.Model.ManifestHash)
            {
                return runtime.CreateResult((await runtime.Store.ApplyAsync(
                    runtime.CreateCommand(action, CommandKind.Evaluate, correlationId) with
                    {
                        NextState = ActionState.Superseded,
                        Code = RuntimeReasonCode.ProposalSuperseded
                    }, cancellationToken)).Action, correlationId);
            }

            // Caller free text is intentionally not persisted. Fixed reason codes prevent accidental secret disclosure.
            var decision = new ApprovalDecision(id, runtime.Principal.Current.PrincipalId, approve, runtime.UtcNow,
                approve ? RuntimeReasonCode.HumanApproved : RuntimeReasonCode.HumanRejected, revision);
            var result = await runtime.Store.ApplyAsync(
                runtime.CreateCommand(action, CommandKind.DecideApproval, correlationId) with
                {
                    ExpectedRevision = revision,
                    Decision = decision,
                    Code = decision.ReasonCode
                }, cancellationToken);
            return runtime.CreateResult(result.Action, correlationId,
                result.Code == StoreResultCode.Ok ? null : result.Code);
        }
        catch (SimulatedCrashException)
        {
            throw;
        }
        catch
        {
            return runtime.CreateResult(action, correlationId, RuntimeReasonCode.PersistenceUnavailable);
        }
    }
}
