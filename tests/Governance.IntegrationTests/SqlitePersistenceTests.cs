using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Persistence.Sqlite.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Governance.IntegrationTests;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task Fresh_database_matches_complete_model_and_migration_snapshot()
    {
        using var fixture = new LegacyDatabaseFixture();
        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await store.InitializeAsync();
        await using var database = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        Assert.False(database.Database.HasPendingModelChanges());
        Assert.Empty(await database.Database.GetPendingMigrationsAsync());
        // Query all tables explicitly: EF's own bookkeeping tables are not domain entities.
        var tables = await database.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();
        Assert.Equal(10, database.Model.GetEntityTypes().Count());
        foreach (var entity in database.Model.GetEntityTypes())
        {
            Assert.Contains(entity.GetTableName()!, tables);
            Assert.DoesNotContain(entity.GetProperties(), property => property.Name == "Json");
        }
        Assert.True(database.Model.FindEntityType(typeof(ActionRow))!.FindProperty(nameof(ActionRow.Revision))!.IsConcurrencyToken);
        Assert.Contains(database.Model.FindEntityType(typeof(ActionRow))!.GetIndexes(),
            index => index.GetDatabaseName() == "IX_Actions_Recovery");
    }

    [Fact]
    public async Task Database_rejects_cross_tenant_history_references()
    {
        using var fixture = new LegacyDatabaseFixture();
        var action = await fixture.SeedAsync(ActionState.Succeeded);
        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await store.InitializeAsync();
        await using var database = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        var other = action with
        {
            TenantId = "other-tenant",
            Proposer = action.Proposer with
            {
                TenantId = "other-tenant"
            },
            Approval = null,
            ApprovalDecision = null,
            Attempt = null,
            LastEvaluation = null,
            Verification = null,
            State = ActionState.Submitted,
            Revision = 0
        };
        Assert.True((await store.InsertProposalAsync(other, "other-proposal", default)).IsSuccess);
        database.ExecutionOutcomes.Add(new ExecutionOutcomeRow
        {
            Id = Guid.NewGuid(),
            TenantId = other.TenantId,
            ActionId = other.ActionId,
            Sequence = 1,
            AttemptId = action.Attempt!.Id,
            Effect = Effect.Applied,
            Code = RuntimeReasonCode.Ok
        });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        Assert.Equal(787, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
        database.ChangeTracker.Clear();
        Assert.Empty(await database.ExecutionOutcomes.Where(row => row.TenantId == other.TenantId).ToListAsync());
    }

    [Fact]
    public async Task Ef_revision_token_rejects_stale_writes()
    {
        using var fixture = new LegacyDatabaseFixture();
        await fixture.SeedAsync(ActionState.AwaitingApproval);
        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await store.InitializeAsync();
        await using var first = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        await using var second = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        var current = await first.Actions.SingleAsync();
        var stale = await second.Actions.SingleAsync();
        current.Revision++;
        await first.SaveChangesAsync();
        stale.Revision++;
        stale.State = ActionState.Cancelled;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await second.Entry(stale).ReloadAsync();
        Assert.Equal(ActionState.AwaitingApproval, stale.State);
    }

    [Fact]
    public async Task Recovery_skips_terminal_history_and_diagnostics_preserve_the_cause()
    {
        using var fixture = new LegacyDatabaseFixture();
        var action = await fixture.SeedAsync(ActionState.Succeeded);
        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await store.InitializeAsync();
        await using var database = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        await database.PolicyEvaluations.ExecuteUpdateAsync(setters => setters.SetProperty(row => row.FactsValuesJson, "broken-json"));
        Assert.Empty(await store.RecoveryCandidatesAsync(default));
        var error = await Assert.ThrowsAsync<PersistenceException>(() => store.FindAsync(action.TenantId, action.ActionId, default));
        Assert.IsAssignableFrom<System.Text.Json.JsonException>(error.InnerException);
        Assert.Equal(nameof(RuntimeReasonCode.PersistenceUnavailable), error.Message);
    }

    [Fact]
    public async Task Cancellation_and_disposal_are_not_misreported_as_storage_failure()
    {
        using var fixture = new LegacyDatabaseFixture();
        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await store.InitializeAsync();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.RecoveryCandidatesAsync(cancellation.Token));
        await store.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => store.FindAsync("tenant-a", Guid.NewGuid(), default));
        await using var nextOwner = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await nextOwner.InitializeAsync();
    }
}
