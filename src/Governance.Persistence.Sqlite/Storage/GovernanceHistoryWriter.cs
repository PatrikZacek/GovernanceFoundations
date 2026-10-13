using System.Text.Json;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Policy;

namespace Governance.Persistence.Sqlite.Storage;

/// <summary>Adds typed append-only records to the caller's unit of work.</summary>
internal static class GovernanceHistoryWriter
{
    public static void Append(RuntimeDb database, GovernedAction action, StoreCommand command)
    {
        if (command.Evaluation is { } evaluation)
        {
            AppendEvaluation(database, action, evaluation);
        }

        if (command.Approval is { } approval)
        {
            database.ApprovalRequests.Add(new ApprovalRequestRow
            {
                Id = approval.Id,
                TenantId = action.TenantId,
                ActionId = action.ActionId,
                Sequence = action.Revision,
                BindingHash = approval.BindingHash,
                Capability = approval.Capability,
                Deadline = approval.Deadline
            });
        }

        if (command.Decision is { } decision)
        {
            database.ApprovalDecisions.Add(new ApprovalDecisionRow
            {
                Id = Guid.NewGuid(),
                TenantId = action.TenantId,
                ActionId = action.ActionId,
                Sequence = action.Revision,
                RequestId = decision.RequestId,
                ApproverId = decision.ApproverId,
                Approved = decision.Approved,
                At = decision.At,
                ReasonCode = decision.ReasonCode,
                Revision = decision.Revision
            });
        }

        if (command.Attempt is { } attempt)
        {
            database.ExecutionAttempts.Add(new ExecutionAttemptRow
            {
                Id = attempt.Id,
                TenantId = action.TenantId,
                ActionId = action.ActionId,
                Sequence = action.Revision,
                ExecutionKey = attempt.ExecutionKey,
                ClaimedAt = attempt.ClaimedAt,
                WorkerId = attempt.WorkerId,
                EvaluationId = attempt.EvaluationId
            });
        }

        if (command.Outcome is { } outcome)
        {
            database.ExecutionOutcomes.Add(new ExecutionOutcomeRow
            {
                Id = Guid.NewGuid(),
                TenantId = action.TenantId,
                ActionId = action.ActionId,
                Sequence = action.Revision,
                AttemptId = action.Attempt!.Id,
                Effect = outcome.Effect,
                Code = outcome.Code,
                ReceiptReference = outcome.ReceiptReference
            });
        }

        if (command.Verification is { } verification)
        {
            database.Verifications.Add(new VerificationRow
            {
                Id = Guid.NewGuid(),
                TenantId = action.TenantId,
                ActionId = action.ActionId,
                Sequence = action.Revision,
                AttemptId = verification.AttemptId,
                At = verification.At,
                Outcome = verification.Result.Outcome,
                Code = verification.Result.Code,
                EvidenceReference = verification.Result.EvidenceReference,
                SourceVersion = verification.Result.SourceVersion
            });
        }
    }

    private static void AppendEvaluation(RuntimeDb database, GovernedAction action, PolicyEvaluation evaluation)
    {
        database.PolicyEvaluations.Add(new PolicyEvaluationRow
        {
            Id = evaluation.Id,
            TenantId = action.TenantId,
            ActionId = action.ActionId,
            Sequence = action.Revision,
            Phase = evaluation.Phase,
            At = evaluation.At,
            ManifestHash = evaluation.ManifestHash,
            FactsVersion = evaluation.Facts.Version,
            FactsValuesJson = JsonSerializer.Serialize(evaluation.Facts.Values),
            Decision = evaluation.Decision
        });

        for (var ordinal = 0; ordinal < evaluation.Results.Length; ordinal++)
        {
            var policy = evaluation.Results[ordinal];
            database.PolicyResults.Add(new PolicyItemRow
            {
                TenantId = action.TenantId,
                ActionId = action.ActionId,
                EvaluationId = evaluation.Id,
                Ordinal = ordinal,
                PolicyId = policy.PolicyId,
                PolicyVersion = policy.PolicyVersion,
                Decision = policy.Decision,
                ReasonCode = policy.ReasonCode,
                SafeMessage = policy.SafeMessage
            });
        }
    }
}
