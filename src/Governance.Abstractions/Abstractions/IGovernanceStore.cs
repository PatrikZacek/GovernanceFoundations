using System.Collections.Immutable;
using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IGovernanceStore : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<StoreResult> InsertProposalAsync(GovernedAction action, string correlationId, CancellationToken ct);
    Task<GovernedAction?> FindAsync(string tenantId, Guid actionId, CancellationToken ct);
    Task<GovernedAction?> FindApprovalAsync(string tenantId, Guid approvalId, CancellationToken ct);
    Task<StoreResult> ApplyAsync(StoreCommand command, CancellationToken ct);
    Task<ImmutableArray<AuditEvent>> ReadHistoryAsync(string tenantId, Guid actionId, long cursor, int take, CancellationToken ct);
    Task<IReadOnlyList<GovernedAction>> RecoveryCandidatesAsync(CancellationToken ct);
    Task RecordIngressAsync(IngressEvent entry, CancellationToken ct);
}

