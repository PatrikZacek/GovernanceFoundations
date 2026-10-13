using System.Text.Json;
using Governance.Abstractions.Approval;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Persistence.Sqlite.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Governance.IntegrationTests;

public sealed class SqliteMigrationTests
{
    [Theory]
    [InlineData(ActionState.AwaitingApproval)]
    [InlineData(ActionState.Executing)]
    [InlineData(ActionState.OutcomeUnknown)]
    [InlineData(ActionState.Succeeded)]
    public async Task Legacy_upgrade_preserves_snapshot_history_and_allows_continuation(ActionState state)
    {
        using var fixture = new LegacyDatabaseFixture();
        var expected = await fixture.SeedAsync(state);
        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await store.InitializeAsync();

        var actual = await store.FindAsync(expected.TenantId, expected.ActionId, default);
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Assert.Equal(expected.ActionId, (await store.FindApprovalAsync(expected.TenantId, expected.Approval!.Id, default))!.ActionId);
        Assert.Null(await store.FindAsync("other-tenant", expected.ActionId, default));
        var history = await store.ReadHistoryAsync(expected.TenantId, expected.ActionId, 0, 100, default);
        Assert.Equal(JsonSerializer.Serialize(fixture.Audit), JsonSerializer.Serialize(history));

        await using var database = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        Assert.False(database.Database.HasPendingModelChanges());
        Assert.Equal(2, (await database.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Equal(fixture.Evaluations.Count, await database.PolicyEvaluations.CountAsync());
        Assert.Equal(fixture.Evaluations.Sum(e => e.Results.Length), await database.PolicyResults.CountAsync());
        Assert.Equal(fixture.Outcomes.Count, await database.ExecutionOutcomes.CountAsync());
        Assert.Equal(fixture.Verifications.Count, await database.Verifications.CountAsync());
        Assert.Equal(fixture.Ingress.Id, (await database.IngressAudit.SingleAsync()).Id);
        Assert.Equal(fixture.Ingress.Code, (await database.IngressAudit.SingleAsync()).Code);
        Assert.Empty(await database.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE name LIKE 'Legacy%'").ToListAsync());
        Assert.Empty(await database.Database.SqlQueryRaw<string>("SELECT \"table\" AS Value FROM pragma_foreign_key_check").ToListAsync());

        if (state == ActionState.AwaitingApproval)
        {
            var decision = new ApprovalDecision(expected.Approval!.Id, "bob", true, DateTimeOffset.UtcNow,
                RuntimeReasonCode.Ok, expected.Revision);
            var result = await store.ApplyAsync(new StoreCommand(CommandKind.DecideApproval, expected.TenantId,
                expected.ActionId, expected.Revision, "bob", "after-upgrade", Decision: decision), default);
            Assert.Equal(ActionState.Ready, result.Action!.State);
            Assert.Equal(decision, (await store.FindAsync(expected.TenantId, expected.ActionId, default))!.ApprovalDecision);
        }
        else if (state == ActionState.Executing)
        {
            Assert.Single(await store.RecoveryCandidatesAsync(default));
            var result = await store.ApplyAsync(new StoreCommand(CommandKind.RecoverClaim, expected.TenantId,
                expected.ActionId, expected.Revision, "recovery", "after-upgrade"), default);
            Assert.Equal(ActionState.OutcomeUnknown, result.Action!.State);
            Assert.Equal(expected.Attempt!.Id, result.Action.Attempt!.Id);
        }
        else if (state == ActionState.OutcomeUnknown)
        {
            var result = await store.ApplyAsync(new StoreCommand(CommandKind.RecordOutcome, expected.TenantId,
                expected.ActionId, expected.Revision, "recovery", "after-upgrade",
                Outcome: new ExecutionResult(Effect.Applied, RuntimeReasonCode.Ok, "receipt-recovered")), default);
            Assert.Equal(ActionState.Verifying, result.Action!.State);
            Assert.Equal("receipt-recovered", (await store.FindAsync(expected.TenantId, expected.ActionId, default))!.Attempt!.Result!.ReceiptReference);
        }
        else
        {
            Assert.Empty(await store.RecoveryCandidatesAsync(default));
        }
    }

    [Fact]
    public async Task Invalid_legacy_reference_rolls_back_schema_and_data_changes()
    {
        using var fixture = new LegacyDatabaseFixture();
        var expected = await fixture.SeedAsync(ActionState.Succeeded);
        await using (var database = new RuntimeDbFactory(fixture.Path).CreateDbContext())
        {
            // The old schema did not enforce the attempt-to-evaluation relationship.
            await database.Database.ExecuteSqlRawAsync("DELETE FROM PolicyEvaluations");
        }

        await using var store = new SqliteGovernanceStore(fixture.Path, TimeProvider.System);
        await Assert.ThrowsAsync<SqliteException>(() => store.InitializeAsync());
        await using var check = new RuntimeDbFactory(fixture.Path).CreateDbContext();
        Assert.Single(await check.Database.GetAppliedMigrationsAsync());
        var original = await check.Database.SqlQueryRaw<string>("SELECT Json AS Value FROM GovernedActions").SingleAsync();
        Assert.Equal(JsonSerializer.Serialize(expected), original);
        Assert.Empty(await check.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE name LIKE 'Legacy%'").ToListAsync());
    }
}

