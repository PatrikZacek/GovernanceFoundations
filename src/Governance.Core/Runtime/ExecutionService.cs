using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Core.Model;

namespace Governance.Core.Runtime;

internal sealed class ExecutionService(
    GovernanceRuntime runtime,
    PolicyEvaluationService evaluation,
    VerificationService verification)
{
    public async Task<GovernanceResult> ExecuteAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        var correlationId = runtime.CreateCorrelationId();
        GovernedAction? action = null;

        try
        {
            action = await runtime.Store.FindAsync(runtime.Principal.Current.TenantId, actionId, cancellationToken);
            if (action is null || !await IsWorkerAuthorizedAsync(action, cancellationToken))
            {
                return new GovernanceResult(RuntimeReasonCode.NotFoundOrUnauthorized, correlationId);
            }

            if (action.State is not (ActionState.Ready or ActionState.AwaitingApproval or ActionState.Submitted))
            {
                return runtime.CreateResult(action, correlationId);
            }

            var definition = runtime.Model.Find(action.ActionTypeId, action.SchemaVersion);
            if (definition is null)
            {
                return runtime.CreateResult(action, correlationId, RuntimeReasonCode.UnknownActionType);
            }

            var evaluated = await evaluation.EvaluateAsync(action, definition, EvaluationPhase.PreExecution,
                correlationId, cancellationToken);
            action = evaluated.Action ?? action;
            if (evaluated.Code != StoreResultCode.Ok || action.State != ActionState.Ready)
            {
                return runtime.CreateResult(action, correlationId,
                    evaluated.Code == StoreResultCode.Ok ? null : evaluated.Code);
            }

            var claim = await ClaimAsync(action, correlationId, cancellationToken);
            action = claim.Action ?? action;
            if (claim.Code != StoreResultCode.Ok)
            {
                return runtime.CreateResult(action, correlationId, claim.Code);
            }

            action = await DispatchAndRecordAsync(action, definition, correlationId, cancellationToken);
            if (action.State == ActionState.Verifying)
            {
                action = await verification.VerifyAsync(action, definition, correlationId, CancellationToken.None);
            }

            return runtime.CreateResult(action, correlationId);
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

    private ValueTask<bool> IsWorkerAuthorizedAsync(GovernedAction action, CancellationToken cancellationToken)
    {
        return runtime.AuthorizeAsync(
            runtime.Principal.Current,
            GovernanceOperation.Execute,
            action.TargetResourceId,
            "governance.execute",
            action.Proposer.PrincipalId,
            cancellationToken);
    }

    private async Task<StoreResult> ClaimAsync(GovernedAction action, string correlationId,
        CancellationToken cancellationToken)
    {
        var attempt = new ExecutionAttempt(
            Guid.NewGuid(), action.ExecutionKey, runtime.UtcNow,
            runtime.Principal.Current.PrincipalId, action.LastEvaluation!.Id);

        var command = runtime.CreateCommand(action, CommandKind.Claim, correlationId) with
        {
            Attempt = attempt,
            Code = RuntimeReasonCode.ExecutionStarted
        };

        try
        {
            return await runtime.Store.ApplyAsync(command, cancellationToken);
        }
        catch (PersistenceException)
        {
            // Executing alone is insufficient: this invocation must own the exact durable attempt.
            var durable = await runtime.Store.FindAsync(action.TenantId, action.ActionId, CancellationToken.None);
            var ownsClaim = durable?.State == ActionState.Executing && durable.Attempt?.Id == attempt.Id;
            return ownsClaim
                ? new StoreResult(StoreResultCode.Ok, durable)
                : new StoreResult(StoreResultCode.PersistenceUnavailable, action);
        }
    }

    private async Task<GovernedAction> DispatchAndRecordAsync(
        GovernedAction action, IActionDefinition definition, string correlationId, CancellationToken cancellationToken)
    {
        runtime.Faults.Hit(GovernanceFaultPoint.AfterClaim);
        var outcome = await InvokeExecutorAsync(action, definition, cancellationToken);
        runtime.Faults.Hit(GovernanceFaultPoint.AfterTargetCommit);

        var command = runtime.CreateCommand(action, CommandKind.RecordOutcome, correlationId) with
        {
            Outcome = outcome,
            Code = outcome.Code
        };

        var recorded = await runtime.Store.ApplyAsync(command, CancellationToken.None);
        if (recorded.Code != StoreResultCode.Ok || recorded.Action is null)
        {
            throw new PersistenceException();
        }

        return recorded.Action;
    }

    private async Task<ExecutionResult> InvokeExecutorAsync(
        GovernedAction action, IActionDefinition definition, CancellationToken cancellationToken)
    {
        try
        {
            return await definition.ExecuteAsync(action, runtime.Services, cancellationToken);
        }
        catch (SimulatedCrashException)
        {
            throw;
        }
        catch
        {
            // Cancellation or timeout is not proof of non-application.
            return new ExecutionResult(Effect.Unknown, RuntimeReasonCode.ExecutionOutcomeUnknown);
        }
    }
}
