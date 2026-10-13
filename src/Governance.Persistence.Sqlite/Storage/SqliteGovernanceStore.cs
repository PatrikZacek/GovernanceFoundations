using System.Collections.Immutable;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Persistence.Sqlite.Hosting;

namespace Governance.Persistence.Sqlite.Storage;

/// <summary>Persistence facade. Lifecycle ownership, reads and transactional writes have separate collaborators.</summary>
public sealed class SqliteGovernanceStore : IGovernanceStore
{
    private readonly SqliteStoreLifetime lifetime;
    private readonly GovernanceReader reader;
    private readonly GovernanceWriter writer;

    public SqliteGovernanceStore(string databasePath, TimeProvider time, IGovernanceFaults? faults = null)
    {
        var path = Path.GetFullPath(databasePath);
        var factory = new RuntimeDbFactory(path);
        lifetime = new SqliteStoreLifetime(new SqliteHostLease(path), new RuntimeDatabaseInitializer(factory));
        reader = new GovernanceReader(factory);
        writer = new GovernanceWriter(factory, time, faults ?? new NoGovernanceFaults());
    }

    public Task InitializeAsync(CancellationToken ct = default) => lifetime.InitializeAsync(ct);

    public Task<StoreResult> InsertProposalAsync(GovernedAction action, string correlationId, CancellationToken ct)
        => lifetime.RunAsync(() => writer.InsertAsync(action, correlationId, ct), ct);

    public Task<StoreResult> ApplyAsync(StoreCommand command, CancellationToken ct)
        => lifetime.RunAsync(() => writer.ApplyAsync(command, ct), ct);

    public Task<GovernedAction?> FindAsync(string tenantId, Guid id, CancellationToken ct)
        => lifetime.RunAsync(() => reader.FindAsync(tenantId, id, ct), ct);

    public Task<GovernedAction?> FindApprovalAsync(string tenantId, Guid id, CancellationToken ct)
        => lifetime.RunAsync(() => reader.FindApprovalAsync(tenantId, id, ct), ct);

    public Task<ImmutableArray<AuditEvent>> ReadHistoryAsync(string tenantId, Guid id, long cursor, int take, CancellationToken ct)
        => lifetime.RunAsync(() => reader.ReadHistoryAsync(tenantId, id, cursor, take, ct), ct);

    public Task<IReadOnlyList<GovernedAction>> RecoveryCandidatesAsync(CancellationToken ct)
        => lifetime.RunAsync(() => reader.RecoveryCandidatesAsync(ct), ct);

    public Task RecordIngressAsync(IngressEvent entry, CancellationToken ct)
        => lifetime.RunAsync(() => writer.RecordIngressAsync(entry, ct), ct);

    public ValueTask DisposeAsync() => lifetime.DisposeAsync();
}
