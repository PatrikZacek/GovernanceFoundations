using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Action;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Core.Hosting;
using Governance.Core.Model;

namespace Governance.Core.Runtime;

internal sealed class GovernanceRuntime(
    GovernanceModel model,
    IGovernanceStore store,
    ITrustedPrincipalAccessor principal,
    IGovernanceAuthorizationService authorization,
    IServiceProvider services,
    TimeProvider time,
    IGovernanceFaults faults,
    GovernanceOptions options
)
{
    public GovernanceModel Model { get; } = model;
    public IGovernanceStore Store { get; } = store;
    public ITrustedPrincipalAccessor Principal { get; } = principal;
    public IGovernanceAuthorizationService Authorization { get; } = authorization;
    public IServiceProvider Services { get; } = services;
    public TimeProvider Time { get; } = time;
    public IGovernanceFaults Faults { get; } = faults;
    public GovernanceOptions Options { get; } = options;
    public DateTimeOffset UtcNow => Time.GetUtcNow();

    public string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public ActionView ToView(GovernedAction action)
    {
        return new ActionView(action.ActionId, action.State, action.Revision, action.Code, action.Approval?.Id,
            action.Approval?.Deadline, action.ProposalFingerprint);
    }

    public GovernanceResult CreateResult(GovernedAction? action, string correlationId, ReasonCode? code = null)
    {
        return new GovernanceResult(code ?? action?.Code ?? RuntimeReasonCode.NotFoundOrUnauthorized, correlationId,
            action is null ? null : ToView(action));
    }

    public ValueTask<bool> AuthorizeAsync(
        TrustedPrincipal proposer,
        GovernanceOperation operation,
        string resource,
        string capability,
        string? owner,
        CancellationToken cancellationToken
    )
    {
        return Authorization.AuthorizeAsync(new AuthorizationRequest(proposer, operation, resource, capability, owner),
            cancellationToken);
    }

    public async Task<bool> IsProposerAuthorizedAsync(GovernedAction action, IActionDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(action.Proposer, GovernanceOperation.Propose, action.TargetResourceId,
                definition.Capability, null, cancellationToken))
        {
            return false;
        }

        return action.Proposer.OnBehalfOfPrincipalId is null
               || await AuthorizeAsync(action.Proposer with
               {
                   PrincipalId = action.Proposer.OnBehalfOfPrincipalId,
                   OnBehalfOfPrincipalId = null,
                   IsHuman = true
               }, GovernanceOperation.Propose, action.TargetResourceId, definition.Capability, null, cancellationToken);
    }

    public async Task<GovernanceResult> RecordIngressFailureAsync(ReasonCode code, string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Store.RecordIngressAsync(
                new IngressEvent(Guid.NewGuid(), Principal.Current.TenantId, UtcNow, correlationId, code),
                cancellationToken);
            return new GovernanceResult(code, correlationId);
        }
        catch (Exception ex) when (ex is not SimulatedCrashException)
        {
            return new GovernanceResult(RuntimeReasonCode.PersistenceUnavailable, correlationId);
        }
    }

    public StoreCommand CreateCommand(GovernedAction action, CommandKind kind, string correlationId)
    {
        return new StoreCommand(kind, action.TenantId, action.ActionId, action.Revision, Principal.Current.PrincipalId,
            correlationId);
    }

    public string CreateApprovalBinding(GovernedAction action)
    {
        return GovernanceJson.Hash(action.ProposalFingerprint + ":" + action.ManifestHash);
    }
}


