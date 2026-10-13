using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Core.Runtime;

internal sealed class ActionCommandService(
    GovernanceRuntime runtime,
    PolicyEvaluationService evaluation,
    ActionQueryService queries)
{
    public async Task<GovernanceResult> CancelAsync(Guid id, long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        var correlationId = runtime.CreateCorrelationId();
        try
        {
            var action = await runtime.Store.FindAsync(runtime.Principal.Current.TenantId, id, cancellationToken);
            if (action is null || !await runtime.AuthorizeAsync(runtime.Principal.Current, GovernanceOperation.Cancel,
                    action.TargetResourceId, "governance.cancel", action.Proposer.PrincipalId, cancellationToken))
            {
                return new GovernanceResult(RuntimeReasonCode.NotFoundOrUnauthorized, correlationId);
            }

            var result = await runtime.Store.ApplyAsync(
                runtime.CreateCommand(action, CommandKind.Cancel, correlationId) with
                {
                    ExpectedRevision = expectedRevision, Code = RuntimeReasonCode.Cancelled
                }, cancellationToken);
            return runtime.CreateResult(result.Action, correlationId,
                result.Code == StoreResultCode.Ok ? null : result.Code);
        }
        catch
        {
            return new GovernanceResult(RuntimeReasonCode.PersistenceUnavailable, correlationId);
        }
    }

    public async Task<GovernanceResult> RetryEvaluationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await queries.GetAsync(id, cancellationToken);
        if (result.Action is null)
        {
            return result;
        }

        try
        {
            var action = (await runtime.Store.FindAsync(runtime.Principal.Current.TenantId, id, cancellationToken))!;
            if (!await runtime.AuthorizeAsync(runtime.Principal.Current, GovernanceOperation.Execute,
                    action.TargetResourceId, "governance.execute", action.Proposer.PrincipalId, cancellationToken))
            {
                return new GovernanceResult(RuntimeReasonCode.Unauthorized, result.CorrelationId);
            }

            var retry = await runtime.Store.ApplyAsync(
                runtime.CreateCommand(action, CommandKind.RetryEvaluation, result.CorrelationId) with
                {
                    Code = RuntimeReasonCode.EvaluationRetry
                }, cancellationToken);
            if (retry.Code != StoreResultCode.Ok)
            {
                return runtime.CreateResult(retry.Action, result.CorrelationId, retry.Code);
            }

            var evaluated = await evaluation.EvaluateAsync(retry.Action!,
                runtime.Model.Find(action.ActionTypeId, action.SchemaVersion)!, EvaluationPhase.Submission,
                result.CorrelationId, cancellationToken);
            return runtime.CreateResult(evaluated.Action, result.CorrelationId,
                evaluated.Code == StoreResultCode.Ok ? null : evaluated.Code);
        }
        catch
        {
            return new GovernanceResult(RuntimeReasonCode.PersistenceUnavailable, result.CorrelationId, result.Action);
        }
    }
}
