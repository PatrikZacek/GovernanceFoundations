using Governance.Abstractions;
using Governance.Abstractions.Action;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Xunit;

namespace Governance.IntegrationTests;

public sealed class RecoveryRetryTests
{
    [Fact]
    public async Task Startup_evaluation_stops_after_three_failed_attempts_without_dispatch()
    {
        await using var scenario = await Scenario.Create();
        scenario.Faults.Arm(GovernanceFaultPoint.AfterSubmission);
        await Assert.ThrowsAsync<SimulatedCrashException>(() => scenario.Submit());
        var proposal = Assert.Single(await scenario.Store.RecoveryCandidatesAsync(default));

        scenario.Behavior.PolicyThrows = true;
        await scenario.Restart();

        var result = await scenario.Get(proposal.ActionId);
        var history = await scenario.As("alice", context => context.GetHistoryAsync(proposal.ActionId));
        Assert.Equal(ActionState.EvaluationFailed, result.Action!.State);
        Assert.Equal(3, history.Events.Count(entry => entry.Type == AuditEventType.Evaluate));
        Assert.Equal(2, history.Events.Count(entry => entry.Type == AuditEventType.RetryEvaluation));
        Assert.Equal(0, scenario.Behavior.DispatchCount);
        Assert.Equal(0, await scenario.Target.ReceiptCountAsync());
    }
}
