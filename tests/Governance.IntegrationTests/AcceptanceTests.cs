using System.Diagnostics;
using System.Text.Json;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Abstractions.Policy;
using Governance.ConfigurationDemo;
using Governance.ConfigurationDemo.Domain;
using Governance.Core.Model;
using Xunit;

namespace Governance.IntegrationTests;

public sealed class AcceptanceTests
{
    [Fact]
    public async Task A01_Development_one_effect_complete_history()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        Assert.Equal(ActionState.Ready, a.Action!.State);
        Assert.Equal(0, await s.Target.ReceiptCountAsync());
        var r = await s.Execute(a.ActionId!.Value);
        Assert.Equal(ActionState.Succeeded, r.Action!.State);
        Assert.Equal(1, await s.Target.ReceiptCountAsync());
        Assert.Equal(new ConfigurationEntry("true", 2), await s.Target.ReadAsync("tenant-a", "development", "feature.new-search"));
        var h = await s.As("alice", g => g.GetHistoryAsync(a.ActionId.Value));
        Assert.Equal(new[] { AuditEventType.ProposalSubmitted, AuditEventType.Evaluate, AuditEventType.Evaluate, AuditEventType.Claim, AuditEventType.RecordOutcome, AuditEventType.RecordVerification }, h.Events.Select(e => e.Type));
        Assert.Equal(Enumerable.Range(1, h.Events.Length).Select(i => (long)i), h.Events.Select(e => e.Sequence));
    }

    [Fact]
    public async Task A02_Production_no_dispatch_without_approval()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit("production");
        Assert.Equal(ActionState.AwaitingApproval, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A03_Bob_approval_and_reevaluation()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit("production");
        Assert.Equal(ActionState.Ready, (await s.Approve(a.ActionId!.Value)).Action!.State);
        Assert.Equal(ActionState.Succeeded, (await s.Execute(a.ActionId.Value)).Action!.State);
        Assert.Equal(1, s.Behavior.DispatchCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A04_Self_and_delegated_approval_forbidden(bool delegated)
    {
        await using var s = await Scenario.Create();
        var a = delegated ? await s.As("service", g => g.SubmitAsync(new UpdateConfigurationValue("production", "feature.new-search", "true", 1), s.Options()), behalf: "alice") : await s.Submit("production");
        var r = await s.As("alice", g => g.ApproveAsync(a.Action!.ApprovalId!.Value, a.Action.Revision, "reviewed"));
        Assert.Equal<ReasonCode>(RuntimeReasonCode.SelfApprovalForbidden, r.Code);
        Assert.Equal(a.Action!.Revision, r.Action!.Revision);
    }

    [Fact]
    public async Task A05_Deny_overrides_approval()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit("production", "auth.secret");
        Assert.Equal(ActionState.Denied, a.Action!.State);
        Assert.Null(a.Action.ApprovalId);
        Assert.Equal(ActionState.Denied, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A06_Malformed_oversized_and_forbidden_input()
    {
        await using var s = await Scenario.Create();
        foreach (var json in new[] { "{bad", new string('x', 65537), "{\"Environment\":\"development\",\"Key\":\"feature.new-search\",\"NewValue\":\"not-boolean\",\"ExpectedVersion\":1}", "{\"Environment\":\"development\",\"Environment\":\"production\"}" })
        {
            var r = await s.As("alice", g => g.SubmitJsonAsync("configuration.update", 1, json, s.Options()));
            Assert.Equal<ReasonCode>(RuntimeReasonCode.InvalidPayload, r.Code);
            Assert.Null(r.Action);
        }
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A07_Cross_tenant_all_operations_hidden()
    {
        await using var s = await Scenario.Create();
        var a = (await s.Submit("production")).Action!;
        var results = new[] { await s.As("mallory", g => g.GetAsync(a.ActionId), "tenant-b"), await s.As("mallory", g => g.ApproveAsync(a.ApprovalId!.Value, a.Revision, "yes"), "tenant-b"), await s.As("mallory", g => g.CancelAsync(a.ActionId, a.Revision), "tenant-b"), await s.As("mallory", g => g.ExecuteAsync(a.ActionId), "tenant-b") };
        Assert.All(results, r => { Assert.Equal<ReasonCode>(RuntimeReasonCode.NotFoundOrUnauthorized, r.Code); Assert.Null(r.Action); });
        var h = await s.As("mallory", g => g.GetHistoryAsync(a.ActionId), "tenant-b");
        Assert.Empty(h.Events);
    }

    [Fact]
    public async Task A08_Unknown_actor_admin_approved_fields_rejected()
    {
        await using var s = await Scenario.Create();
        var json = "{\"Environment\":\"development\",\"Key\":\"feature.new-search\",\"NewValue\":\"true\",\"ExpectedVersion\":1,\"Actor\":\"worker\",\"Admin\":true,\"Approved\":true}";
        Assert.Equal<ReasonCode>(RuntimeReasonCode.InvalidPayload, (await s.As("alice", g => g.SubmitJsonAsync("configuration.update", 1, json, s.Options()))).Code);
        var a = await s.Submit();
        Assert.Equal<ReasonCode>(RuntimeReasonCode.NotFoundOrUnauthorized, (await s.As("alice", g => g.ExecuteAsync(a.ActionId!.Value))).Code);
    }

    [Fact]
    public async Task A09_Concurrent_idempotency_single_action()
    {
        await using var s = await Scenario.Create();
        var o = s.Options();
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() => s.Submit(options: o)));
        var r = await Task.WhenAll(tasks);
        Assert.Single(r.Select(x => x.ActionId).Distinct());
        Assert.DoesNotContain(r, r => r.ActionId is null);
        var h = await s.As("alice", g => g.GetHistoryAsync(r[0].ActionId!.Value));
        Assert.Single(h.Events, e => e.Type == AuditEventType.ProposalSubmitted);
    }

    [Fact]
    public async Task A10_Idempotency_payload_and_delegation_conflict()
    {
        await using var s = await Scenario.Create();
        var o = s.Options();
        var a = await s.Submit(options: o);
        var b = await s.Submit(value: "false", options: o);
        Assert.Equal<ReasonCode>(RuntimeReasonCode.IdempotencyConflict, b.Code);
        Assert.Equal(a.ActionId, b.ActionId);
        var d = s.Options();
        var p = new UpdateConfigurationValue("development", "feature.new-search", "true", 1);
        await s.As("service", g => g.SubmitAsync(p, d));
        Assert.Equal<ReasonCode>(RuntimeReasonCode.IdempotencyConflict, (await s.As("service", g => g.SubmitAsync(p, d), behalf: "alice")).Code);
    }

    [Fact]
    public async Task A11_Twenty_concurrent_executions_one_claim_receipt()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => s.Execute(a.ActionId!.Value))));
        Assert.Equal(1, s.Behavior.DispatchCount);
        Assert.Equal(1, await s.Target.ReceiptCountAsync());
        Assert.Equal(ActionState.Succeeded, (await s.Get(a.ActionId!.Value)).Action!.State);
    }

    [Fact]
    public async Task A12_Proposer_revoked_before_execution()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Auth.Revoke("alice", "configuration.propose");
        Assert.Equal(ActionState.Denied, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A13_Approver_revoked_before_execution()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit("production");
        await s.Approve(a.ActionId!.Value);
        s.Auth.Revoke("bob", "configuration.approve");
        Assert.Equal(ActionState.Denied, (await s.Execute(a.ActionId.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A14_Exact_deadline_blocks_dispatch(bool approval)
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit(approval ? "production" : "development");
        if (approval)
        {
            await s.Approve(a.ActionId!.Value);
        }

        s.Time.Advance(approval ? TimeSpan.FromMinutes(30) : TimeSpan.FromHours(1));
        Assert.Equal(ActionState.Expired, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A15_Manifest_change_supersedes_approval()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit("production");
        await s.Approve(a.ActionId!.Value);
        await s.Restart("configuration-demo/2");
        Assert.Equal(ActionState.Superseded, (await s.Execute(a.ActionId.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A16_Target_changes_between_evaluation_and_write_CAS_preserves_change()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Behavior.BeforeWrite = () => s.Target.ForceExternalChangeAsync("development", "feature.new-search", "false");
        Assert.Equal(ActionState.Superseded, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(new ConfigurationEntry("false", 2), await s.Target.ReadAsync("tenant-a", "development", "feature.new-search"));
        Assert.Equal(0, await s.Target.ReceiptCountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A17_Policy_exception_or_timeout_fails_closed(bool timeout)
    {
        await using var s = await Scenario.Create();
        s.Behavior.PolicyThrows = !timeout;
        s.Behavior.PolicyTimeout = timeout;
        var a = await s.Submit();
        Assert.Equal(ActionState.EvaluationFailed, a.Action!.State);
        await s.Execute(a.ActionId!.Value);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A18_Audit_failure_before_claim_commit_rolls_back()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Faults.Arm(GovernanceFaultPoint.BeforeClaimCommit, new PersistenceException());
        Assert.Equal<ReasonCode>(RuntimeReasonCode.PersistenceUnavailable, (await s.Execute(a.ActionId!.Value)).Code);
        Assert.Equal(0, s.Behavior.DispatchCount);
        var h = await s.As("alice", g => g.GetHistoryAsync(a.ActionId!.Value));
        Assert.DoesNotContain(h.Events, e => e.Type == AuditEventType.Claim);
        Assert.Equal(ActionState.Ready, (await s.Get(a.ActionId!.Value)).Action!.State);
    }

    [Fact]
    public async Task A19_Restart_after_target_commit_reconciles_without_second_write()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Faults.Arm(GovernanceFaultPoint.AfterTargetCommit);
        await Assert.ThrowsAsync<SimulatedCrashException>(() => s.Execute(a.ActionId!.Value));
        Assert.Equal(1, await s.Target.ReceiptCountAsync());
        await s.Restart();
        Assert.Equal(ActionState.Succeeded, (await s.Get(a.ActionId!.Value)).Action!.State);
        Assert.Equal(1, s.Behavior.DispatchCount);
        Assert.Equal(1, await s.Target.ReceiptCountAsync());
    }

    [Fact]
    public async Task A20_Unknown_outcome_never_blindly_retries_write()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Behavior.UnknownExecution = true;
        Assert.Equal(ActionState.OutcomeUnknown, (await s.Execute(a.ActionId!.Value)).Action!.State);
        await s.Execute(a.ActionId.Value);
        await s.Restart();
        Assert.Equal(ActionState.OutcomeUnknown, (await s.Get(a.ActionId.Value)).Action!.State);
        Assert.Equal(1, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A21_Verification_crash_only_repeats_read()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Faults.Arm(GovernanceFaultPoint.DuringVerification);
        await Assert.ThrowsAsync<SimulatedCrashException>(() => s.Execute(a.ActionId!.Value));
        Assert.Equal(ActionState.Verifying, (await s.Get(a.ActionId!.Value)).Action!.State);
        await s.Restart();
        Assert.Equal(ActionState.Succeeded, (await s.Get(a.ActionId.Value)).Action!.State);
        Assert.Equal(1, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A22_Mismatch_is_not_success_or_rollback()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Behavior.VerificationMismatch = true;
        Assert.Equal(ActionState.VerificationFailed, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(new ConfigurationEntry("true", 2), await s.Target.ReadAsync("tenant-a", "development", "feature.new-search"));
    }

    [Fact]
    public async Task A23_Approval_survives_restart_unchanged()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit("production");
        var before = await s.As("alice", g => g.GetHistoryAsync(a.ActionId!.Value));
        await s.Restart();
        var after = await s.Get(a.ActionId!.Value);
        Assert.Equal(a.Action, after.Action);
        Assert.Equal(before.Events.ToArray(), (await s.As("alice", g => g.GetHistoryAsync(a.ActionId!.Value))).Events.ToArray());
    }

    [Fact]
    public async Task A24_Concurrent_approve_reject_single_decision_and_replay()
    {
        await using var s = await Scenario.Create();
        var a = (await s.Submit("production")).Action!;
        var results = await Task.WhenAll(Task.Run(() => s.As("bob", g => g.ApproveAsync(a.ApprovalId!.Value, a.Revision, "yes"))), Task.Run(() => s.As("bob", g => g.RejectAsync(a.ApprovalId!.Value, a.Revision, "no"))));
        Assert.Single(results, r => r.Code == RuntimeReasonCode.ConcurrencyConflict);
        var now = (await s.Get(a.ActionId)).Action!;
        var replay = now.State == ActionState.Ready ? await s.As("bob", g => g.ApproveAsync(a.ApprovalId!.Value, a.Revision, "again")) : await s.As("bob", g => g.RejectAsync(a.ApprovalId!.Value, a.Revision, "again"));
        Assert.Equal(now.Revision, replay.Action!.Revision);
    }

    [Fact]
    public async Task A25_Cancel_execution_race_has_single_winner()
    {
        await using var s = await Scenario.Create();
        var a = (await s.Submit()).Action!;
        var r = await Task.WhenAll(Task.Run(() => s.Execute(a.ActionId)), Task.Run(() => s.As("alice", g => g.CancelAsync(a.ActionId, a.Revision))));
        var final = (await s.Get(a.ActionId)).Action!;
        Assert.Contains(final.State, new[] { ActionState.Cancelled, ActionState.Succeeded });
        Assert.Equal(final.State == ActionState.Cancelled ? 0 : 1, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A26_Unknown_type_schema_and_bad_registration()
    {
        await using var s = await Scenario.Create();
        Assert.Equal<ReasonCode>(RuntimeReasonCode.UnknownActionType, (await s.As("alice", g => g.SubmitAsync(42, s.Options()))).Code);
        Assert.Equal<ReasonCode>(RuntimeReasonCode.UnsupportedSchema, (await s.As("alice", g => g.SubmitJsonAsync("configuration.update", 99, "{}", s.Options()))).Code);
        var b = new GovernanceBuilder().ModelVersion("bad");
        b.Action<int>("broken", 1);
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public async Task A27_Canary_not_in_ingress_history_or_exception_result()
    {
        await using var s = await Scenario.Create();
        const string canary = "SECRET_CANARY_NEVER_LOG";
        var invalid = await s.Submit(value: canary);
        Assert.DoesNotContain(canary, JsonSerializer.Serialize(invalid));
        s.Behavior.PolicyThrows = true;
        var a = await s.Submit();
        var h = await s.As("alice", g => g.GetHistoryAsync(a.ActionId!.Value));
        Assert.DoesNotContain(canary, JsonSerializer.Serialize(h));
        var bytes = await File.ReadAllBytesAsync(Path.Combine(s.Directory, "governance.db"));
        Assert.DoesNotContain(canary, System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task A28_Preview_does_not_persist_or_grant_execution()
    {
        await using var s = await Scenario.Create();
        var r = await s.As("alice", g => g.PreviewAsync(new UpdateConfigurationValue("production", "feature.new-search", "true", 1)));
        Assert.False(r.IsBinding);
        Assert.Equal(PolicyDecision.RequireApproval, r.Evaluation!.Decision);
        Assert.Empty(await s.Store.RecoveryCandidatesAsync(default));
        Assert.Equal(0, await s.Target.ReceiptCountAsync());
        Assert.Equal<ReasonCode>(RuntimeReasonCode.NotFoundOrUnauthorized, (await s.Execute(r.Evaluation.ActionId)).Code);
    }

    [Fact]
    public async Task A29_Second_process_refuses_same_store()
    {
        await using var s = await Scenario.Create();
        var dll = typeof(DemoHost).Assembly.Location;
        var p = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        p.ArgumentList.Add(dll);
        p.ArgumentList.Add("--hold-lock");
        p.ArgumentList.Add(s.Directory);
        using var child = Process.Start(p)!;
        var output = child.StandardOutput.ReadToEndAsync();
        var error = child.StandardError.ReadToEndAsync();
        await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
        Assert.NotEqual(0, child.ExitCode);
        Assert.Contains("GovernanceHostAlreadyRunning", await error);
        Assert.DoesNotContain("LOCK_READY", await output);
    }

    [Fact]
    public async Task A30_Target_advanced_is_inconclusive_not_overwritten()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Faults.Arm(GovernanceFaultPoint.DuringVerification);
        await Assert.ThrowsAsync<SimulatedCrashException>(() => s.Execute(a.ActionId!.Value));
        await s.Target.ForceExternalChangeAsync("development", "feature.new-search", "false");
        await s.Restart();
        var r = await s.Get(a.ActionId!.Value);
        Assert.Equal(ActionState.OutcomeUnknown, r.Action!.State);
        Assert.Equal(ConfigurationReasonCodes.TargetAdvanced, r.Code);
        Assert.Equal(new ConfigurationEntry("false", 3), await s.Target.ReadAsync("tenant-a", "development", "feature.new-search"));
        Assert.Equal(1, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task A31_Ambiguous_claim_commit_verifies_attempt_ownership()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Faults.Arm(GovernanceFaultPoint.AfterClaimCommit, new PersistenceException());
        Assert.Equal(ActionState.Succeeded, (await s.Execute(a.ActionId!.Value)).Action!.State);
        Assert.Equal(1, s.Behavior.DispatchCount);
        Assert.Equal(1, await s.Target.ReceiptCountAsync());
    }

    [Fact]
    public async Task A32_Submitted_crash_recovers_original_action()
    {
        await using var s = await Scenario.Create();
        var options = s.Options();
        s.Faults.Arm(GovernanceFaultPoint.AfterSubmission);
        await Assert.ThrowsAsync<SimulatedCrashException>(() => s.Submit(options: options));
        var candidate = Assert.Single(await s.Store.RecoveryCandidatesAsync(default));
        Assert.Equal(ActionState.Submitted, candidate.State);
        await s.Restart();
        var r = await s.Submit(options: options);
        Assert.Equal(candidate.ActionId, r.ActionId);
        Assert.Equal(ActionState.Ready, r.Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task New_deny_after_approval_and_dynamic_approval_are_enforced()
    {
        await using var s = await Scenario.Create();
        var a = await s.Submit();
        s.Behavior.RequireDevelopmentApproval = true;
        Assert.Equal(ActionState.AwaitingApproval, (await s.Execute(a.ActionId!.Value)).Action!.State);
        await s.Approve(a.ActionId.Value);
        s.Behavior.DenyAll = true;
        Assert.Equal(ActionState.Denied, (await s.Execute(a.ActionId.Value)).Action!.State);
        Assert.Equal(0, s.Behavior.DispatchCount);
    }

    [Fact]
    public async Task Explicit_policy_retry_records_new_attempt()
    {
        await using var s = await Scenario.Create();
        s.Behavior.PolicyThrows = true;
        var a = await s.Submit();
        s.Behavior.PolicyThrows = false;
        var r = await s.As("worker", g => g.RetryEvaluationAsync(a.ActionId!.Value));
        Assert.Equal(ActionState.Ready, r.Action!.State);
        var h = await s.As("alice", g => g.GetHistoryAsync(a.ActionId!.Value));
        Assert.Contains(h.Events, e => e.Type == AuditEventType.RetryEvaluation);
    }
}



