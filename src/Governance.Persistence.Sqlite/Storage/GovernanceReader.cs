using System.Collections.Immutable;
using System.Text.Json;
using Governance.Abstractions.Approval;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;
using Governance.Persistence.Sqlite.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Storage;

internal sealed class GovernanceReader(IDbContextFactory<RuntimeDb> factory)
{
    public async Task<GovernedAction?> FindAsync(string tenantId, Guid actionId, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        var row = await database.Actions.AsNoTracking().SingleOrDefaultAsync(
            action => action.TenantId == tenantId && action.ActionId == actionId, cancellationToken);
        return row is null ? null : await ReadAsync(database, row, cancellationToken);
    }

    public async Task<GovernedAction?> FindApprovalAsync(string tenantId, Guid approvalId, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        var row = await database.Actions.AsNoTracking().SingleOrDefaultAsync(
            action => action.TenantId == tenantId && action.ApprovalId == approvalId, cancellationToken);
        return row is null ? null : await ReadAsync(database, row, cancellationToken);
    }

    public async Task<ImmutableArray<AuditEvent>> ReadHistoryAsync(
        string tenantId, Guid actionId, long cursor, int take, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        var rows = await database.Audit.AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.ActionId == actionId && row.Sequence > cursor)
            .OrderBy(row => row.Sequence).Take(take).ToListAsync(cancellationToken);
        return rows.Select(AuditRowMapper.Read).ToImmutableArray();
    }

    public async Task<IReadOnlyList<GovernedAction>> RecoveryCandidatesAsync(CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        // The indexed lifecycle filter runs in SQLite, before any history or JSON facts are loaded.
        var rows = await database.Actions.AsNoTracking()
            .Where(row => row.State == ActionState.Submitted || row.State == ActionState.Executing
                || row.State == ActionState.Verifying || row.State == ActionState.OutcomeUnknown)
            .OrderBy(row => row.TenantId).ThenBy(row => row.ActionId).ToListAsync(cancellationToken);
        var actions = new List<GovernedAction>(rows.Count);
        foreach (var row in rows)
        {
            actions.Add(await ReadAsync(database, row, cancellationToken));
        }

        return actions;
    }

    public static async Task<GovernedAction> ReadAsync(RuntimeDb database, ActionRow row, CancellationToken cancellationToken)
    {
        var evaluation = await ReadEvaluationAsync(database, row, cancellationToken);
        var approval = await Latest<ApprovalRequestRow>(database, row).FirstOrDefaultAsync(cancellationToken);
        var decision = await Latest<ApprovalDecisionRow>(database, row).FirstOrDefaultAsync(cancellationToken);
        var attempt = await Latest<ExecutionAttemptRow>(database, row).FirstOrDefaultAsync(cancellationToken);
        var outcome = await Latest<ExecutionOutcomeRow>(database, row).FirstOrDefaultAsync(cancellationToken);
        var verification = await Latest<VerificationRow>(database, row).FirstOrDefaultAsync(cancellationToken);

        if (row.ApprovalId != approval?.Id)
        {
            throw new InvalidDataException("Stored approval reference does not match its history.");
        }

        return new GovernedAction(
            row.ActionId, row.TenantId, row.ActionTypeId, row.SchemaVersion,
            row.PayloadJson, row.PayloadFingerprint, row.ProposalFingerprint,
            new TrustedPrincipal(row.TenantId, row.ProposerId, row.ProposerIsHuman, row.OnBehalfOfPrincipalId, row.AgentId),
            row.TargetResourceId, row.ExpectedTargetVersion, row.IdempotencyKey, row.SubmittedAt, row.ExpiresAt,
            row.ManifestHash, row.State, row.Revision, row.Code,
            approval is null ? null : new ApprovalRequest(approval.Id, approval.BindingHash, approval.Capability, approval.Deadline),
            decision is null ? null : new ApprovalDecision(decision.RequestId, decision.ApproverId, decision.Approved,
                decision.At, decision.ReasonCode, decision.Revision),
            attempt is null ? null : new ExecutionAttempt(attempt.Id, attempt.ExecutionKey, attempt.ClaimedAt,
                attempt.WorkerId, attempt.EvaluationId,
                outcome is null ? null : new ExecutionResult(outcome.Effect, outcome.Code, outcome.ReceiptReference)),
            evaluation,
            verification is null ? null : new VerificationRecord(verification.AttemptId, verification.At,
                new VerificationResult(verification.Outcome, verification.Code, verification.EvidenceReference, verification.SourceVersion)));
    }

    private static IOrderedQueryable<T> Latest<T>(RuntimeDb database, ActionRow action) where T : class, IActionHistoryRow
    {
        return database.Set<T>().AsNoTracking()
            .Where(row => row.TenantId == action.TenantId && row.ActionId == action.ActionId)
            .OrderByDescending(row => row.Sequence);
    }

    private static async Task<PolicyEvaluation?> ReadEvaluationAsync(RuntimeDb database, ActionRow action, CancellationToken cancellationToken)
    {
        var evaluation = await Latest<PolicyEvaluationRow>(database, action).FirstOrDefaultAsync(cancellationToken);
        if (evaluation is null)
        {
            return null;
        }

        var rows = await database.PolicyResults.AsNoTracking()
            .Where(row => row.TenantId == action.TenantId && row.ActionId == action.ActionId && row.EvaluationId == evaluation.Id)
            .OrderBy(row => row.Ordinal).ToListAsync(cancellationToken);
        var values = JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(evaluation.FactsValuesJson)
            ?? throw new InvalidDataException("Stored facts must be a JSON object.");
        return new PolicyEvaluation(evaluation.Id, action.TenantId, action.ActionId, evaluation.Phase,
            evaluation.At, evaluation.ManifestHash, new FactsSnapshot(evaluation.FactsVersion, values),
            rows.Select(row => new PolicyItem(row.PolicyId, row.PolicyVersion, row.Decision, row.ReasonCode, row.SafeMessage)).ToImmutableArray(),
            evaluation.Decision);
    }
}
