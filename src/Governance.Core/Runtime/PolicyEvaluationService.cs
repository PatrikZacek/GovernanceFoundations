using Governance.Abstractions.Approval;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;
using Governance.Core.Model;

namespace Governance.Core.Runtime;

internal sealed class PolicyEvaluationService(GovernanceRuntime runtime)
{
    public async Task<StoreResult> EvaluateAsync(GovernedAction action, IActionDefinition definition,
        EvaluationPhase phase, string correlationId, CancellationToken cancellationToken)
    {
        ActionState? blocked = null;
        ReasonCode code = RuntimeReasonCode.Ok;
        if (action.ManifestHash != runtime.Model.ManifestHash)
        {
            blocked = ActionState.Superseded;
            code = RuntimeReasonCode.ProposalSuperseded;
        }
        else if (runtime.UtcNow >= action.ExpiresAt ||
                 (action.Approval is not null && runtime.UtcNow >= action.Approval.Deadline))
        {
            blocked = ActionState.Expired;
            code = runtime.UtcNow >= action.ExpiresAt
                ? RuntimeReasonCode.ProposalExpired
                : RuntimeReasonCode.ApprovalExpired;
        }
        else if (!await runtime.IsProposerAuthorizedAsync(action, definition, cancellationToken)
                 || action.ApprovalDecision?.Approved == true && (!await runtime.AuthorizeAsync(
                         new TrustedPrincipal(action.TenantId,
                             action.ApprovalDecision.ApproverId),
                         GovernanceOperation.Approve, action.TargetResourceId,
                         action.Approval!.Capability,
                         action.Proposer.PrincipalId, cancellationToken) ||
                     action.Approval.BindingHash !=
                     runtime.CreateApprovalBinding(action)))
        {
            blocked = ActionState.Denied;
            code = RuntimeReasonCode.Unauthorized;
        }

        if (blocked is not null)
        {
            return await runtime.Store.ApplyAsync(
                runtime.CreateCommand(action, CommandKind.Evaluate, correlationId) with
                {
                    NextState = blocked, Code = code
                }, cancellationToken);
        }

        var policyEvaluation = await definition.EvaluateAsync(action, phase, runtime.UtcNow, runtime.Services,
            runtime.Options.PolicyTimeout, cancellationToken);
        var state = policyEvaluation.Decision switch
        {
            PolicyDecision.Deny => ActionState.Denied,
            PolicyDecision.Unavailable => ActionState.EvaluationFailed,
            PolicyDecision.RequireApproval when action.ApprovalDecision?.Approved != true => ActionState
                .AwaitingApproval,
            _ => ActionState.Ready
        };
        ApprovalRequest? approval = null;
        if (state == ActionState.AwaitingApproval && action.Approval is null)
        {
            if (definition.Approval is null)
            {
                state = ActionState.EvaluationFailed;
            }
            else
            {
                approval = new ApprovalRequest(Guid.NewGuid(), runtime.CreateApprovalBinding(action),
                    definition.Approval.Capability,
                    new[] { action.ExpiresAt, runtime.UtcNow + definition.Approval.Lifetime }.Min());
            }
        }

        code = state switch
        {
            ActionState.Denied => RuntimeReasonCode.PolicyDenied,
            ActionState.EvaluationFailed => RuntimeReasonCode.PolicyUnavailable,
            ActionState.AwaitingApproval => RuntimeReasonCode.ApprovalRequired,
            _ => RuntimeReasonCode.Ready
        };
        return await runtime.Store.ApplyAsync(
            runtime.CreateCommand(action, CommandKind.Evaluate, correlationId) with
            {
                NextState = state, Code = code, Evaluation = policyEvaluation, Approval = approval
            }, cancellationToken);
    }
}
