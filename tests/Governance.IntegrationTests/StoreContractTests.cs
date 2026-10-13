using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Action;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Persistence.Sqlite.Storage;
using Governance.Testing.Domain;
using Xunit;

namespace Governance.IntegrationTests;

public sealed class StoreContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Providers_enforce_idempotency_revision_tenant_and_atomic_audit(bool sqlite)
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        await using IGovernanceStore store = sqlite
            ? new SqliteGovernanceStore(Path.Combine(Path.GetTempPath(), "governance-contract-" + Guid.NewGuid(), "runtime.db"), time)
            : new MemoryGovernanceStore(time);
        await store.InitializeAsync();

        var proposal = new GovernedAction(
            Guid.NewGuid(), "tenant-a", "contract.action", 1, "{}", "payload-hash", "proposal-hash",
            new TrustedPrincipal("tenant-a", "proposer"), "resource", 1, "same-key",
            time.GetUtcNow(), time.GetUtcNow().AddHours(1), "manifest");

        var submissions = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => store.InsertProposalAsync(proposal with { ActionId = Guid.NewGuid() }, "submission", CancellationToken.None))));
        var actionId = Assert.Single(submissions.Select(result => result.Action!.ActionId).Distinct());
        var conflict = await store.InsertProposalAsync(proposal with
        {
            ProposalFingerprint = "different"
        }, "conflict", CancellationToken.None);
        Assert.Equal(StoreResultCode.IdempotencyConflict, conflict.Code);
        Assert.Null(await store.FindAsync("tenant-b", actionId, CancellationToken.None));

        var cancel = new StoreCommand(CommandKind.Cancel, "tenant-a", actionId, 0, "proposer", "cancel", Code: RuntimeReasonCode.Cancelled);
        var cancelled = await store.ApplyAsync(cancel, CancellationToken.None);
        Assert.Equal(ActionState.Cancelled, cancelled.Action!.State);
        Assert.Equal(StoreResultCode.ConcurrencyConflict, (await store.ApplyAsync(cancel, CancellationToken.None)).Code);
        Assert.Equal(StoreResultCode.NotFoundOrUnauthorized, (await store.ApplyAsync(cancel with
        {
            TenantId = "tenant-b"
        }, CancellationToken.None)).Code);

        var history = await store.ReadHistoryAsync("tenant-a", actionId, 0, 10, CancellationToken.None);
        Assert.Equal([1, 2], history.Select(entry => entry.Sequence));
        Assert.Empty(await store.ReadHistoryAsync("tenant-b", actionId, 0, 10, CancellationToken.None));
    }
}

