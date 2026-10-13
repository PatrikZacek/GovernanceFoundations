using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governance.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeGovernanceStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "GovernedActions", newName: "LegacyGovernedActions");
            migrationBuilder.RenameTable(name: "IngressAuditEvents", newName: "LegacyIngressAuditEvents");
            migrationBuilder.RenameTable(name: "ApprovalRequests", newName: "LegacyApprovalRequests");
            migrationBuilder.RenameTable(name: "AuditEvents", newName: "LegacyAuditEvents");
            migrationBuilder.RenameTable(name: "PolicyEvaluations", newName: "LegacyPolicyEvaluations");
            migrationBuilder.RenameTable(name: "ApprovalDecisions", newName: "LegacyApprovalDecisions");
            migrationBuilder.RenameTable(name: "ExecutionAttempts", newName: "LegacyExecutionAttempts");
            migrationBuilder.RenameTable(name: "ExecutionOutcomes", newName: "LegacyExecutionOutcomes");
            migrationBuilder.RenameTable(name: "VerificationRecords", newName: "LegacyVerificationRecords");

            migrationBuilder.CreateTable(
                name: "IngressAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngressAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApproverId = table.Column<string>(type: "TEXT", nullable: false),
                    Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.Id);
                    table.UniqueConstraint("AK_ApprovalDecisions_TenantId_ActionId_Id", x => new { x.TenantId, x.ActionId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    BindingHash = table.Column<string>(type: "TEXT", nullable: false),
                    Capability = table.Column<string>(type: "TEXT", nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.UniqueConstraint("AK_ApprovalRequests_TenantId_ActionId_Id", x => new { x.TenantId, x.ActionId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "GovernedActions",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProposerId = table.Column<string>(type: "TEXT", nullable: false),
                    ProposerIsHuman = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnBehalfOfPrincipalId = table.Column<string>(type: "TEXT", nullable: true),
                    AgentId = table.Column<string>(type: "TEXT", nullable: true),
                    ActionTypeId = table.Column<string>(type: "TEXT", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadFingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    ProposalFingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    TargetResourceId = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedTargetVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ManifestHash = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernedActions", x => new { x.TenantId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_GovernedActions_ApprovalRequests_TenantId_ActionId_ApprovalId",
                        columns: x => new { x.TenantId, x.ActionId, x.ApprovalId },
                        principalTable: "ApprovalRequests",
                        principalColumns: new[] { "TenantId", "ActionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    SafeMetadataJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_GovernedActions_TenantId_ActionId",
                        columns: x => new { x.TenantId, x.ActionId },
                        principalTable: "GovernedActions",
                        principalColumns: new[] { "TenantId", "ActionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PolicyEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    Phase = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ManifestHash = table.Column<string>(type: "TEXT", nullable: false),
                    FactsVersion = table.Column<string>(type: "TEXT", nullable: false),
                    FactsValuesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyEvaluations", x => x.Id);
                    table.UniqueConstraint("AK_PolicyEvaluations_TenantId_ActionId_Id", x => new { x.TenantId, x.ActionId, x.Id });
                    table.ForeignKey(
                        name: "FK_PolicyEvaluations_GovernedActions_TenantId_ActionId",
                        columns: x => new { x.TenantId, x.ActionId },
                        principalTable: "GovernedActions",
                        principalColumns: new[] { "TenantId", "ActionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    ExecutionKey = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    WorkerId = table.Column<string>(type: "TEXT", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionAttempts", x => x.Id);
                    table.UniqueConstraint("AK_ExecutionAttempts_TenantId_ActionId_Id", x => new { x.TenantId, x.ActionId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExecutionAttempts_GovernedActions_TenantId_ActionId",
                        columns: x => new { x.TenantId, x.ActionId },
                        principalTable: "GovernedActions",
                        principalColumns: new[] { "TenantId", "ActionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionAttempts_PolicyEvaluations_TenantId_ActionId_EvaluationId",
                        columns: x => new { x.TenantId, x.ActionId, x.EvaluationId },
                        principalTable: "PolicyEvaluations",
                        principalColumns: new[] { "TenantId", "ActionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PolicyResults",
                columns: table => new
                {
                    EvaluationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", nullable: false),
                    SafeMessage = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyResults", x => new { x.EvaluationId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_PolicyResults_PolicyEvaluations_TenantId_ActionId_EvaluationId",
                        columns: x => new { x.TenantId, x.ActionId, x.EvaluationId },
                        principalTable: "PolicyEvaluations",
                        principalColumns: new[] { "TenantId", "ActionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Effect = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiptReference = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionOutcomes", x => x.Id);
                    table.UniqueConstraint("AK_ExecutionOutcomes_TenantId_ActionId_Id", x => new { x.TenantId, x.ActionId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExecutionOutcomes_ExecutionAttempts_TenantId_ActionId_AttemptId",
                        columns: x => new { x.TenantId, x.ActionId, x.AttemptId },
                        principalTable: "ExecutionAttempts",
                        principalColumns: new[] { "TenantId", "ActionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionOutcomes_GovernedActions_TenantId_ActionId",
                        columns: x => new { x.TenantId, x.ActionId },
                        principalTable: "GovernedActions",
                        principalColumns: new[] { "TenantId", "ActionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceReference = table.Column<string>(type: "TEXT", nullable: true),
                    SourceVersion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationRecords", x => x.Id);
                    table.UniqueConstraint("AK_VerificationRecords_TenantId_ActionId_Id", x => new { x.TenantId, x.ActionId, x.Id });
                    table.ForeignKey(
                        name: "FK_VerificationRecords_ExecutionAttempts_TenantId_ActionId_AttemptId",
                        columns: x => new { x.TenantId, x.ActionId, x.AttemptId },
                        principalTable: "ExecutionAttempts",
                        principalColumns: new[] { "TenantId", "ActionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationRecords_GovernedActions_TenantId_ActionId",
                        columns: x => new { x.TenantId, x.ActionId },
                        principalTable: "GovernedActions",
                        principalColumns: new[] { "TenantId", "ActionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_TenantId_ActionId_RequestId",
                table: "ApprovalDecisions",
                columns: new[] { "TenantId", "ActionId", "RequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_TenantId_ActionId_Sequence",
                table: "ApprovalDecisions",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_TenantId_ActionId_Sequence",
                table: "ApprovalRequests",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Audit_Sequence",
                table: "AuditEvents",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionAttempts_TenantId_ActionId_EvaluationId",
                table: "ExecutionAttempts",
                columns: new[] { "TenantId", "ActionId", "EvaluationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionAttempts_TenantId_ActionId_Sequence",
                table: "ExecutionAttempts",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionOutcomes_TenantId_ActionId_AttemptId",
                table: "ExecutionOutcomes",
                columns: new[] { "TenantId", "ActionId", "AttemptId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionOutcomes_TenantId_ActionId_Sequence",
                table: "ExecutionOutcomes",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actions_Recovery",
                table: "GovernedActions",
                columns: new[] { "State", "TenantId", "ActionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GovernedActions_TenantId_ActionId_ApprovalId",
                table: "GovernedActions",
                columns: new[] { "TenantId", "ActionId", "ApprovalId" });

            migrationBuilder.CreateIndex(
                name: "UX_Actions_Approval",
                table: "GovernedActions",
                columns: new[] { "TenantId", "ApprovalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Actions_Idempotency",
                table: "GovernedActions",
                columns: new[] { "TenantId", "ProposerId", "ActionTypeId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvaluations_TenantId_ActionId_Sequence",
                table: "PolicyEvaluations",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyResults_TenantId_ActionId_EvaluationId",
                table: "PolicyResults",
                columns: new[] { "TenantId", "ActionId", "EvaluationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRecords_TenantId_ActionId_AttemptId",
                table: "VerificationRecords",
                columns: new[] { "TenantId", "ActionId", "AttemptId" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRecords_TenantId_ActionId_Sequence",
                table: "VerificationRecords",
                columns: new[] { "TenantId", "ActionId", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecisions_ApprovalRequests_TenantId_ActionId_RequestId",
                table: "ApprovalDecisions",
                columns: new[] { "TenantId", "ActionId", "RequestId" },
                principalTable: "ApprovalRequests",
                principalColumns: new[] { "TenantId", "ActionId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecisions_GovernedActions_TenantId_ActionId",
                table: "ApprovalDecisions",
                columns: new[] { "TenantId", "ActionId" },
                principalTable: "GovernedActions",
                principalColumns: new[] { "TenantId", "ActionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_GovernedActions_TenantId_ActionId",
                table: "ApprovalRequests",
                columns: new[] { "TenantId", "ActionId" },
                principalTable: "GovernedActions",
                principalColumns: new[] { "TenantId", "ActionId" },
                onDelete: ReferentialAction.Restrict);

            // Legacy history used independent storage IDs and JSON domain IDs. Preserve domain
            // references and append order; historical sequence numbers are per record type.
            // All schema and data changes run in the EF migration transaction.
            migrationBuilder.Sql("""
            INSERT INTO GovernedActions (
                TenantId,
                ActionId,
                ProposerId,
                ProposerIsHuman,
                OnBehalfOfPrincipalId,
                AgentId,
                ActionTypeId,
                SchemaVersion,
                PayloadJson,
                PayloadFingerprint,
                ProposalFingerprint,
                TargetResourceId,
                ExpectedTargetVersion,
                IdempotencyKey,
                SubmittedAt,
                ExpiresAt,
                ManifestHash,
                State,
                Revision,
                Code,
                ApprovalId)
            SELECT
                l.TenantId,
                upper(l.ActionId),
                l.ProposerId,
                json_extract(l.Json, '$.Proposer.IsHuman'),
                json_extract(l.Json, '$.Proposer.OnBehalfOfPrincipalId'),
                json_extract(l.Json, '$.Proposer.AgentId'),
                l.ActionTypeId,
                json_extract(l.Json, '$.SchemaVersion'),
                json_extract(l.Json, '$.PayloadJson'),
                json_extract(l.Json, '$.PayloadFingerprint'),
                json_extract(l.Json, '$.ProposalFingerprint'),
                json_extract(l.Json, '$.TargetResourceId'),
                json_extract(l.Json, '$.ExpectedTargetVersion'),
                l.IdempotencyKey,
                json_extract(l.Json, '$.SubmittedAt'),
                json_extract(l.Json, '$.ExpiresAt'),
                json_extract(l.Json, '$.ManifestHash'),
                CASE json_extract(l.Json, '$.State')
                    WHEN 0 THEN 'Submitted'
                    WHEN 1 THEN 'EvaluationFailed'
                    WHEN 2 THEN 'AwaitingApproval'
                    WHEN 3 THEN 'Ready'
                    WHEN 4 THEN 'Executing'
                    WHEN 5 THEN 'Verifying'
                    WHEN 6 THEN 'OutcomeUnknown'
                    WHEN 7 THEN 'Succeeded'
                    WHEN 8 THEN 'Denied'
                    WHEN 9 THEN 'Rejected'
                    WHEN 10 THEN 'Expired'
                    WHEN 11 THEN 'Cancelled'
                    WHEN 12 THEN 'Superseded'
                    WHEN 13 THEN 'Failed'
                    WHEN 14 THEN 'VerificationFailed'
                    ELSE json_extract(l.Json, '$.State')
                END,
                l.Revision,
                json_extract(l.Json, '$.Code'),
                NULL
            FROM LegacyGovernedActions l;

            INSERT INTO IngressAuditEvents (
                Id,
                TenantId,
                At,
                CorrelationId,
                Code)
            SELECT
                upper(l.Id),
                l.TenantId,
                json_extract(l.Json, '$.At'),
                json_extract(l.Json, '$.CorrelationId'),
                json_extract(l.Json, '$.Code')
            FROM LegacyIngressAuditEvents l;

            INSERT INTO AuditEvents (
                Id,
                TenantId,
                ActionId,
                Sequence,
                Type,
                At,
                ActorId,
                CorrelationId,
                Code,
                SafeMetadataJson)
            SELECT
                upper(l.Id),
                l.TenantId,
                upper(l.ActionId),
                l.Sequence,
                json_extract(l.Json, '$.Type'),
                json_extract(l.Json, '$.At'),
                json_extract(l.Json, '$.ActorId'),
                json_extract(l.Json, '$.CorrelationId'),
                json_extract(l.Json, '$.Code'),
                COALESCE(json_extract(l.Json, '$.SafeMetadataJson'), '{}')
            FROM LegacyAuditEvents l;

            INSERT INTO PolicyEvaluations (
                Id,
                TenantId,
                ActionId,
                Sequence,
                Phase,
                At,
                ManifestHash,
                FactsVersion,
                FactsValuesJson,
                Decision)
            SELECT
                upper(json_extract(l.Json, '$.Id')),
                l.TenantId,
                upper(l.ActionId),
                ROW_NUMBER() OVER (PARTITION BY l.TenantId, l.ActionId ORDER BY l.rowid),
                CASE json_extract(l.Json, '$.Phase')
                    WHEN 'preview' THEN 'Preview'
                    WHEN 'submission' THEN 'Submission'
                    WHEN 'pre-execution' THEN 'PreExecution'
                    ELSE json_extract(l.Json, '$.Phase')
                END,
                json_extract(l.Json, '$.At'),
                json_extract(l.Json, '$.ManifestHash'),
                json_extract(l.Json, '$.Facts.Version'),
                json_extract(l.Json, '$.Facts.Values'),
                CASE json_extract(l.Json, '$.Decision')
                    WHEN 0 THEN 'Allow'
                    WHEN 1 THEN 'Deny'
                    WHEN 2 THEN 'RequireApproval'
                    WHEN 3 THEN 'Unavailable'
                    ELSE json_extract(l.Json, '$.Decision')
                END
            FROM LegacyPolicyEvaluations l;

            INSERT INTO PolicyResults (
                EvaluationId,
                Ordinal,
                TenantId,
                ActionId,
                PolicyId,
                PolicyVersion,
                Decision,
                ReasonCode,
                SafeMessage)
            SELECT
                upper(json_extract(l.Json, '$.Id')),
                p.key,
                l.TenantId,
                upper(l.ActionId),
                json_extract(p.value, '$.PolicyId'),
                json_extract(p.value, '$.PolicyVersion'),
                CASE json_extract(p.value, '$.Decision')
                    WHEN 0 THEN 'Allow'
                    WHEN 1 THEN 'Deny'
                    WHEN 2 THEN 'RequireApproval'
                    WHEN 3 THEN 'Unavailable'
                    ELSE json_extract(p.value, '$.Decision')
                END,
                json_extract(p.value, '$.ReasonCode'),
                json_extract(p.value, '$.SafeMessage')
            FROM LegacyPolicyEvaluations l, json_each(l.Json, '$.Results') p;

            INSERT INTO ApprovalRequests (
                Id,
                TenantId,
                ActionId,
                Sequence,
                BindingHash,
                Capability,
                Deadline)
            SELECT
                upper(json_extract(l.Json, '$.Id')),
                l.TenantId,
                upper(l.ActionId),
                ROW_NUMBER() OVER (PARTITION BY l.TenantId, l.ActionId ORDER BY l.rowid),
                json_extract(l.Json, '$.BindingHash'),
                json_extract(l.Json, '$.Capability'),
                json_extract(l.Json, '$.Deadline')
            FROM LegacyApprovalRequests l;

            INSERT INTO ApprovalDecisions (
                Id,
                TenantId,
                ActionId,
                Sequence,
                RequestId,
                ApproverId,
                Approved,
                At,
                ReasonCode,
                Revision)
            SELECT
                upper(l.Id),
                l.TenantId,
                upper(l.ActionId),
                ROW_NUMBER() OVER (PARTITION BY l.TenantId, l.ActionId ORDER BY l.rowid),
                upper(json_extract(l.Json, '$.RequestId')),
                json_extract(l.Json, '$.ApproverId'),
                json_extract(l.Json, '$.Approved'),
                json_extract(l.Json, '$.At'),
                json_extract(l.Json, '$.ReasonCode'),
                json_extract(l.Json, '$.Revision')
            FROM LegacyApprovalDecisions l;

            INSERT INTO ExecutionAttempts (
                Id,
                TenantId,
                ActionId,
                Sequence,
                ExecutionKey,
                ClaimedAt,
                WorkerId,
                EvaluationId)
            SELECT
                upper(json_extract(l.Json, '$.Id')),
                l.TenantId,
                upper(l.ActionId),
                ROW_NUMBER() OVER (PARTITION BY l.TenantId, l.ActionId ORDER BY l.rowid),
                json_extract(l.Json, '$.ExecutionKey'),
                json_extract(l.Json, '$.ClaimedAt'),
                json_extract(l.Json, '$.WorkerId'),
                upper(json_extract(l.Json, '$.EvaluationId'))
            FROM LegacyExecutionAttempts l;

            INSERT INTO ExecutionOutcomes (
                Id,
                TenantId,
                ActionId,
                Sequence,
                AttemptId,
                Effect,
                Code,
                ReceiptReference)
            SELECT
                upper(l.Id),
                l.TenantId,
                upper(l.ActionId),
                ROW_NUMBER() OVER (PARTITION BY l.TenantId, l.ActionId ORDER BY l.rowid),
                upper(json_extract(a.Json, '$.Attempt.Id')),
                CASE json_extract(l.Json, '$.Effect')
                    WHEN 0 THEN 'Applied'
                    WHEN 1 THEN 'NotApplied'
                    WHEN 2 THEN 'Unknown'
                    ELSE json_extract(l.Json, '$.Effect')
                END,
                json_extract(l.Json, '$.Code'),
                json_extract(l.Json, '$.ReceiptReference')
            FROM LegacyExecutionOutcomes l JOIN LegacyGovernedActions a ON a.TenantId = l.TenantId AND a.ActionId = l.ActionId;

            INSERT INTO VerificationRecords (
                Id,
                TenantId,
                ActionId,
                Sequence,
                AttemptId,
                At,
                Outcome,
                Code,
                EvidenceReference,
                SourceVersion)
            SELECT
                upper(l.Id),
                l.TenantId,
                upper(l.ActionId),
                ROW_NUMBER() OVER (PARTITION BY l.TenantId, l.ActionId ORDER BY l.rowid),
                upper(json_extract(l.Json, '$.AttemptId')),
                json_extract(l.Json, '$.At'),
                CASE json_extract(l.Json, '$.Result.Outcome')
                    WHEN 0 THEN 'Verified'
                    WHEN 1 THEN 'Mismatch'
                    WHEN 2 THEN 'Inconclusive'
                    ELSE json_extract(l.Json, '$.Result.Outcome')
                END,
                json_extract(l.Json, '$.Result.Code'),
                json_extract(l.Json, '$.Result.EvidenceReference'),
                json_extract(l.Json, '$.Result.SourceVersion')
            FROM LegacyVerificationRecords l;

            UPDATE GovernedActions SET ApprovalId = (SELECT upper(l.ApprovalId) FROM LegacyGovernedActions l WHERE l.TenantId = GovernedActions.TenantId AND upper(l.ActionId) = GovernedActions.ActionId);
            """);

            migrationBuilder.DropTable(name: "LegacyVerificationRecords");
            migrationBuilder.DropTable(name: "LegacyExecutionOutcomes");
            migrationBuilder.DropTable(name: "LegacyExecutionAttempts");
            migrationBuilder.DropTable(name: "LegacyApprovalDecisions");
            migrationBuilder.DropTable(name: "LegacyPolicyEvaluations");
            migrationBuilder.DropTable(name: "LegacyAuditEvents");
            migrationBuilder.DropTable(name: "LegacyApprovalRequests");
            migrationBuilder.DropTable(name: "LegacyIngressAuditEvents");
            migrationBuilder.DropTable(name: "LegacyGovernedActions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Destructive downgrade is not supported. Restore a pre-upgrade backup instead.");
    }
}
