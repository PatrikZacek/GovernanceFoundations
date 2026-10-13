using System.Collections.Immutable;
using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Action;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Core;

namespace Governance.Testing.Domain;

public sealed class MemoryGovernanceStore(TimeProvider time, IGovernanceFaults? faults = null) : IGovernanceStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<(string, Guid), GovernedAction> actions = [];
    private readonly List<AuditEvent> history = [];
    private readonly List<IngressEvent> ingress = [];
    public IReadOnlyList<IngressEvent> Ingress
    {
        get
        {
            lock (gate)
            {
                return ingress.ToArray();
            }
        }
    }
    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<StoreResult> InsertProposalAsync(GovernedAction a, string correlation, CancellationToken ct)
    {
        lock (gate)
        {
            var existing = actions.Values.FirstOrDefault(x => x.TenantId == a.TenantId && x.Proposer.PrincipalId == a.Proposer.PrincipalId && x.ActionTypeId == a.ActionTypeId && x.IdempotencyKey == a.IdempotencyKey);
            if (existing is not null)
            {
                return Task.FromResult(new StoreResult(existing.ProposalFingerprint == a.ProposalFingerprint ? StoreResultCode.Ok : StoreResultCode.IdempotencyConflict, existing));
            }
            faults?.Hit(GovernanceFaultPoint.BeforeSubmissionCommit);
            actions.Add((a.TenantId, a.ActionId), a);
            history.Add(GovernanceTransitions.Event(a, AuditEventType.ProposalSubmitted, a.Proposer.PrincipalId, correlation, time.GetUtcNow()));
            return Task.FromResult(new StoreResult(StoreResultCode.Ok, a));
        }
    }
    public Task<GovernedAction?> FindAsync(string tenantId, Guid id, CancellationToken ct)
    {
        lock (gate)
        {
            return Task.FromResult(actions.GetValueOrDefault((tenantId, id)));
        }
    }
    public Task<GovernedAction?> FindApprovalAsync(string tenantId, Guid id, CancellationToken ct)
    {
        lock (gate)
        {
            return Task.FromResult(actions.Values.FirstOrDefault(a => a.TenantId == tenantId && a.Approval?.Id == id));
        }
    }
    public Task<StoreResult> ApplyAsync(StoreCommand c, CancellationToken ct)
    {
        lock (gate)
        {
            if (!actions.TryGetValue((c.TenantId, c.ActionId), out var a))
            {
                return Task.FromResult(new StoreResult(StoreResultCode.NotFoundOrUnauthorized, null));
            }
            var r = GovernanceTransitions.Apply(a, c, time.GetUtcNow());
            if (r.Code != StoreResultCode.Ok || r.Action!.Revision == a.Revision)
            {
                return Task.FromResult(r);
            }
            faults?.Hit(c.Kind == CommandKind.Claim ? GovernanceFaultPoint.BeforeClaimCommit : GovernanceFaultPoint.BeforeTransitionCommit);
            actions[(c.TenantId, c.ActionId)] = r.Action;
            history.Add(GovernanceTransitions.Event(r.Action, AuditEventTypes.FromCommand(c.Kind), c.ActorId, c.CorrelationId, time.GetUtcNow()));
            if (c.Kind == CommandKind.Claim)
            {
                faults?.Hit(GovernanceFaultPoint.AfterClaimCommit);
            }
            return Task.FromResult(r);
        }
    }
    public Task<ImmutableArray<AuditEvent>> ReadHistoryAsync(string tenantId, Guid id, long cursor, int take, CancellationToken ct)
    {
        lock (gate)
        {
            return Task.FromResult(history.Where(e => e.TenantId == tenantId && e.ActionId == id && e.Sequence > cursor).OrderBy(e => e.Sequence).Take(take).ToImmutableArray());
        }
    }
    public Task<IReadOnlyList<GovernedAction>> RecoveryCandidatesAsync(CancellationToken ct)
    {
        lock (gate)
        {
            return Task.FromResult<IReadOnlyList<GovernedAction>>(actions.Values.Where(a => a.State is ActionState.Submitted or ActionState.Executing or ActionState.Verifying or ActionState.OutcomeUnknown).ToArray());
        }
    }
    public Task RecordIngressAsync(IngressEvent e, CancellationToken ct)
    {
        lock (gate)
        {
            ingress.Add(e);
            return Task.CompletedTask;
        }
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
