using Governance.Abstractions;
using Governance.Abstractions.Action;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Core.Model;

namespace Governance.Core.Runtime;

internal sealed class RecoveryService(GovernanceRuntime runtime, PolicyEvaluationService evaluation, VerificationService verification)
{
    public async Task RecoverAsync(CancellationToken cancellationToken)
    {
        foreach (var candidate in await runtime.Store.RecoveryCandidatesAsync(cancellationToken))
        {
            var action = candidate;
            var definition = runtime.Model.Find(action.ActionTypeId, action.SchemaVersion);
            if (definition is null)
            {
                throw new InvalidOperationException("Recovery requires registered historical action type.");
            }

            var correlationId = runtime.CreateCorrelationId();
            if (action.State == ActionState.Submitted)
            {
                await RecoverSubmittedAsync(action, definition, correlationId, cancellationToken);
                continue;
            }

            if (action.State == ActionState.Executing)
            {
                action = (await runtime.Store.ApplyAsync(runtime.CreateCommand(action, CommandKind.RecoverClaim, correlationId) with
                {
                    Code = RuntimeReasonCode.ExecutionOutcomeUnknown
                }, cancellationToken)).Action!;
            }

            if (action.State == ActionState.OutcomeUnknown)
            {
                action = await GetEvidence(definition, correlationId, action, cancellationToken);
            }

            if (action.State == ActionState.Verifying)
            {
                await verification.VerifyAsync(action, definition, correlationId, cancellationToken);
            }
        }
    }

    private async Task<GovernedAction> GetEvidence(IActionDefinition definition, string correlationId,
        GovernedAction action, CancellationToken cancellationToken)
    {
        ExecutionResult evidence;
        try
        {
            evidence = await definition.ReconcileAsync(action, runtime.Services, cancellationToken);
        }
        catch (SimulatedCrashException)
        {
            throw;
        }
        catch
        {
            return action;
        }

        if (evidence.Effect == Effect.Unknown)
        {
            return action;
        }

        action = (await runtime.Store.ApplyAsync(runtime.CreateCommand(action, CommandKind.RecordOutcome, correlationId)
            with { Outcome = evidence, Code = evidence.Code }, cancellationToken)).Action!;
        return action;
    }

    private async Task RecoverSubmittedAsync(GovernedAction action, IActionDefinition definition, string correlationId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= runtime.Options.MaxRecoveryEvaluationAttempts; attempt++)
        {
            var result = await evaluation.EvaluateAsync(action, definition, EvaluationPhase.Submission, correlationId, cancellationToken);
            if (result.Code != StoreResultCode.Ok || result.Action is null)
            {
                throw new PersistenceException();
            }

            action = result.Action;
            if (action.State != ActionState.EvaluationFailed || attempt == runtime.Options.MaxRecoveryEvaluationAttempts)
            {
                return;
            }

            var retry = runtime.CreateCommand(action, CommandKind.RetryEvaluation, correlationId) with
            {
                Code = RuntimeReasonCode.RecoveryEvaluationRetry
            };
            result = await runtime.Store.ApplyAsync(retry, cancellationToken);
            if (result.Code != StoreResultCode.Ok || result.Action is null)
            {
                throw new PersistenceException();
            }

            action = result.Action;
        }
    }
}


