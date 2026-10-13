using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Core.Hosting;
using Governance.Core.Model;
using Governance.Core.Runtime;

namespace Governance.Core;
/// <summary>Scoped application facade. Submission and execution are separate explicit boundaries.</summary>
public sealed class GovernanceContext
{
    private readonly ProposalService proposals;
    private readonly ApprovalService approvals;
    private readonly ExecutionService executions;
    private readonly ActionQueryService queries;
    private readonly ActionCommandService commands;
    private readonly RecoveryService recovery;

    public GovernanceContext(
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
        var runtime = new GovernanceRuntime(model, store, principal, authorization, services, time, faults, options);
        var evaluation = new PolicyEvaluationService(runtime);
        var verification = new VerificationService(runtime);
        proposals = new ProposalService(runtime, evaluation);
        approvals = new ApprovalService(runtime);
        executions = new ExecutionService(runtime, evaluation, verification);
        queries = new ActionQueryService(runtime);
        commands = new ActionCommandService(runtime, evaluation, queries);
        recovery = new RecoveryService(runtime, evaluation, verification);
    }

    public Task<GovernanceResult> SubmitAsync<T>(
        T payload,
        SubmissionOptions options,
        CancellationToken ct = default) => proposals.SubmitAsync(payload, options, ct);
    public Task<GovernanceResult> SubmitJsonAsync(
        string actionTypeId,
        int schemaVersion,
        string json,
        SubmissionOptions options,
        CancellationToken ct = default) => proposals.SubmitJsonAsync(actionTypeId, schemaVersion, json, options, ct);
    public Task<PreviewResult> PreviewAsync<T>(
        T payload,
        CancellationToken ct = default) => proposals.PreviewAsync(payload, ct);
    public Task<GovernanceResult> ApproveAsync(
        Guid approvalId,
        long expectedRevision,
        string reason,
        CancellationToken ct = default) => approvals.ApproveAsync(approvalId, expectedRevision, reason, ct);
    public Task<GovernanceResult> RejectAsync(
        Guid approvalId,
        long expectedRevision,
        string reason,
        CancellationToken ct = default) => approvals.RejectAsync(approvalId, expectedRevision, reason, ct);
    public Task<GovernanceResult> ExecuteAsync(
        Guid actionId,
        CancellationToken ct = default) => executions.ExecuteAsync(actionId, ct);
    public Task<GovernanceResult> GetAsync(
        Guid actionId,
        CancellationToken ct = default) => queries.GetAsync(actionId, ct);
    public Task<HistoryPage> GetHistoryAsync(
        Guid actionId,
        long cursor = 0,
        int pageSize = 50,
        CancellationToken ct = default) => queries.GetHistoryAsync(actionId, cursor, pageSize, ct);
    public Task<GovernanceResult> CancelAsync(
        Guid actionId,
        long expectedRevision,
        CancellationToken ct = default) => commands.CancelAsync(actionId, expectedRevision, ct);
    public Task<GovernanceResult> RetryEvaluationAsync(
        Guid actionId,
        CancellationToken ct = default) => commands.RetryEvaluationAsync(actionId, ct);
    internal Task RecoverAsync(CancellationToken ct) => recovery.RecoverAsync(ct);
}

