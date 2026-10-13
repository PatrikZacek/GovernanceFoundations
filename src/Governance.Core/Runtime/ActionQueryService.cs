using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Core.Runtime;

internal sealed class ActionQueryService(GovernanceRuntime runtime)
{
    public async Task<GovernanceResult> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var correlationId = runtime.CreateCorrelationId();
        try
        {
            var action = await runtime.Store.FindAsync(runtime.Principal.Current.TenantId, id, cancellationToken);
            return action is not null
                   && await runtime.AuthorizeAsync(
                       runtime.Principal.Current,
                       GovernanceOperation.Read,
                       action.TargetResourceId,
                       "governance.read",
                       action.Proposer.PrincipalId,
                       cancellationToken
                   )
                ? runtime.CreateResult(action, correlationId)
                : new GovernanceResult(RuntimeReasonCode.NotFoundOrUnauthorized, correlationId);
        }
        catch
        {
            return new GovernanceResult(RuntimeReasonCode.PersistenceUnavailable, correlationId);
        }
    }

    public async Task<HistoryPage> GetHistoryAsync(Guid id, long cursor = 0, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await GetAsync(id, cancellationToken);
        if (result.Action is null)
        {
            return new HistoryPage(result.Code, result.CorrelationId, [], null);
        }

        try
        {
            var events = await runtime.Store.ReadHistoryAsync(runtime.Principal.Current.TenantId, id,
                Math.Max(0, cursor), Math.Clamp(pageSize, 1, 100), cancellationToken);
            return new HistoryPage(RuntimeReasonCode.Ok, result.CorrelationId, events,
                events.Length == Math.Clamp(pageSize, 1, 100) ? events[^1].Sequence : null);
        }
        catch
        {
            return new HistoryPage(RuntimeReasonCode.PersistenceUnavailable, result.CorrelationId, [], null);
        }
    }
}
