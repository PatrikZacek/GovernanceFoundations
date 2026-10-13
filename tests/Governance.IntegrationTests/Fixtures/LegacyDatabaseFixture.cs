using System.Collections.Immutable;
using System.Text.Json;
using Governance.Abstractions.Approval;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;
using Governance.Persistence.Sqlite.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Governance.IntegrationTests;

/// <summary>Writes the released JSON storage format, independently of the new EF mappings.</summary>
internal sealed class LegacyDatabaseFixture : IDisposable
{
    private const string InitialMigration = "202609120001_InitialGovernance";
    private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "governance-migration-" + Guid.NewGuid());
    public string Path => System.IO.Path.Combine(directory, "runtime.db");
    public List<PolicyEvaluation> Evaluations { get; } = [];
    public List<ExecutionResult> Outcomes { get; } = [];
    public List<VerificationRecord> Verifications { get; } = [];
    public List<AuditEvent> Audit { get; } = [];
    public IngressEvent Ingress { get; private set; } = null!;

    public async Task<GovernedAction> SeedAsync(ActionState state)
    {
        Directory.CreateDirectory(directory);
        await using var database = new RuntimeDbFactory(Path).CreateDbContext();
        // Preserve the original (12-digit) migration ID already recorded by existing databases.
        // EF's target lookup expects 14 digits, so resolve its historical name through EF.
        var initialName = database.GetService<IMigrationsIdGenerator>().GetName(InitialMigration);
        await database.GetService<IMigrator>().MigrateAsync(initialName);
        var at = DateTimeOffset.UtcNow;
        var actionId = Guid.NewGuid();
        var approval = new ApprovalRequest(Guid.NewGuid(), "binding-fingerprint", "approve.production", at.AddHours(1));
        var action = new GovernedAction(actionId, "tenant-a", "migration.action", 1, "{\"value\":\"žluťoučký\"}",
            "payload-fingerprint", "proposal-fingerprint", new TrustedPrincipal("tenant-a", "service", false, "alice", "agent-1"),
            "configuration/resource", 42, "original-key", at, at.AddHours(2), "manifest", State: state, Revision: 20,
            Code: RuntimeReasonCode.Ok, Approval: approval);
        Evaluations.Add(Evaluation(action, EvaluationPhase.Submission, PolicyDecision.RequireApproval, at));
        if (state != ActionState.AwaitingApproval)
        {
            Evaluations.Add(Evaluation(action, EvaluationPhase.PreExecution, PolicyDecision.RequireApproval, at));
            action = action with
            {
                ApprovalDecision = new ApprovalDecision(approval.Id, "bob", true, at, RuntimeReasonCode.Ok, 2),
                Attempt = new ExecutionAttempt(Guid.NewGuid(), action.ExecutionKey, at, "worker", Evaluations[^1].Id)
            };
        }

        if (state is ActionState.OutcomeUnknown or ActionState.Succeeded)
        {
            Outcomes.Add(new ExecutionResult(Effect.Unknown, RuntimeReasonCode.Ok));
            if (state == ActionState.Succeeded)
            {
                Outcomes.Add(new ExecutionResult(Effect.Applied, RuntimeReasonCode.Ok, "receipt-original"));
                Verifications.Add(new VerificationRecord(action.Attempt!.Id, at,
                    new VerificationResult(VerificationOutcome.Inconclusive, RuntimeReasonCode.Ok)));
                Verifications.Add(new VerificationRecord(action.Attempt.Id, at,
                    new VerificationResult(VerificationOutcome.Verified, RuntimeReasonCode.Ok, "evidence-original", "version-43")));
            }

            action = action with
            {
                Attempt = action.Attempt! with
                {
                    Result = Outcomes[^1]
                },
                Verification = Verifications.LastOrDefault()
            };
        }

        action = action with
        {
            LastEvaluation = Evaluations[^1]
        };
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO GovernedActions (TenantId,ActionId,ProposerId,ActionTypeId,IdempotencyKey,Revision,ApprovalId,Json)
            VALUES ({action.TenantId},{action.ActionId},{action.Proposer.PrincipalId},{action.ActionTypeId},{action.IdempotencyKey},{action.Revision},{approval.Id},{JsonSerializer.Serialize(action)})
            """);
        foreach (var evaluation in Evaluations)
        {
            await AppendAsync(database, "PolicyEvaluations", action, evaluation);
        }
        await AppendAsync(database, "ApprovalRequests", action, approval);
        if (action.ApprovalDecision is { } decision)
        {
            await AppendAsync(database, "ApprovalDecisions", action, decision);
        }
        if (action.Attempt is { } attempt)
        {
            await AppendAsync(database, "ExecutionAttempts", action, attempt with
            {
                Result = null
            });
        }
        foreach (var outcome in Outcomes)
        {
            await AppendAsync(database, "ExecutionOutcomes", action, outcome);
        }
        foreach (var verification in Verifications)
        {
            await AppendAsync(database, "VerificationRecords", action, verification);
        }
        Audit.Add(new AuditEvent(Guid.NewGuid(), action.TenantId, action.ActionId, 1, AuditEventType.ProposalSubmitted,
            at, "service", "original-submission", RuntimeReasonCode.Ok)
        {
            SafeMetadataJson = "{\"safe\":true}"
        });
        Audit.Add(new AuditEvent(Guid.NewGuid(), action.TenantId, action.ActionId, action.Revision + 1, AuditEventType.Evaluate,
            at, "worker", "original-transition", RuntimeReasonCode.Ok));
        foreach (var entry in Audit)
        {
            await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AuditEvents (Id,TenantId,ActionId,Sequence,Json) VALUES ({entry.Id},{entry.TenantId},{entry.ActionId},{entry.Sequence},{JsonSerializer.Serialize(entry)})");
        }
        Ingress = new IngressEvent(Guid.NewGuid(), action.TenantId, at, "ingress-original", RuntimeReasonCode.Ok);
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO IngressAuditEvents (Id,TenantId,Json) VALUES ({Ingress.Id},{Ingress.TenantId},{JsonSerializer.Serialize(Ingress)})");
        return action;
    }

    private static PolicyEvaluation Evaluation(GovernedAction action, EvaluationPhase phase, PolicyDecision decision, DateTimeOffset at)
        => new(Guid.NewGuid(), action.TenantId, action.ActionId, phase, at, action.ManifestHash,
            new FactsSnapshot("facts-42", ImmutableDictionary<string, string>.Empty.Add("environment", "production")),
            [new PolicyItem("test.policy", "1", decision, RuntimeReasonCode.Ok, "Requires independent review")], decision);

    private static Task AppendAsync<T>(RuntimeDb database, string table, GovernedAction action, T record)
    {
        var sql = table switch
        {
            "PolicyEvaluations" => "INSERT INTO PolicyEvaluations (Id,TenantId,ActionId,Json) VALUES ({0},{1},{2},{3})",
            "ApprovalRequests" => "INSERT INTO ApprovalRequests (Id,TenantId,ActionId,Json) VALUES ({0},{1},{2},{3})",
            "ApprovalDecisions" => "INSERT INTO ApprovalDecisions (Id,TenantId,ActionId,Json) VALUES ({0},{1},{2},{3})",
            "ExecutionAttempts" => "INSERT INTO ExecutionAttempts (Id,TenantId,ActionId,Json) VALUES ({0},{1},{2},{3})",
            "ExecutionOutcomes" => "INSERT INTO ExecutionOutcomes (Id,TenantId,ActionId,Json) VALUES ({0},{1},{2},{3})",
            "VerificationRecords" => "INSERT INTO VerificationRecords (Id,TenantId,ActionId,Json) VALUES ({0},{1},{2},{3})",
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        return database.Database.ExecuteSqlRawAsync(sql,
            Guid.NewGuid(), action.TenantId, action.ActionId, JsonSerializer.Serialize(record));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
