using Governance.Persistence.Sqlite.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Governance.Persistence.Sqlite.Migrations;

[DbContext(typeof(RuntimeDb))]
[Migration("202609120001_InitialGovernance")]
public sealed class InitialGovernance : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""
        CREATE TABLE GovernedActions (TenantId TEXT NOT NULL, ActionId TEXT NOT NULL, ProposerId TEXT NOT NULL, ActionTypeId TEXT NOT NULL, IdempotencyKey TEXT NOT NULL, Revision INTEGER NOT NULL, ApprovalId TEXT NULL, Json TEXT NOT NULL, PRIMARY KEY(TenantId,ActionId));
        CREATE UNIQUE INDEX IX_GovernedActions_TenantId_ProposerId_ActionTypeId_IdempotencyKey ON GovernedActions(TenantId,ProposerId,ActionTypeId,IdempotencyKey);
        CREATE UNIQUE INDEX IX_GovernedActions_TenantId_ApprovalId ON GovernedActions(TenantId,ApprovalId);
        CREATE TABLE AuditEvents (Id TEXT NOT NULL PRIMARY KEY, TenantId TEXT NOT NULL, ActionId TEXT NOT NULL, Sequence INTEGER NOT NULL, Json TEXT NOT NULL, FOREIGN KEY(TenantId,ActionId) REFERENCES GovernedActions(TenantId,ActionId));
        CREATE UNIQUE INDEX IX_AuditEvents_TenantId_ActionId_Sequence ON AuditEvents(TenantId,ActionId,Sequence);
        CREATE TABLE IngressAuditEvents (Id TEXT NOT NULL PRIMARY KEY, TenantId TEXT NOT NULL, Json TEXT NOT NULL);
        """);
        foreach (var table in new[]
        {
            "PolicyEvaluations",
            "ApprovalRequests",
            "ApprovalDecisions",
            "ExecutionAttempts",
            "ExecutionOutcomes",
            "VerificationRecords"
        }

        )
        {
            m.Sql($"CREATE TABLE {table} (Id TEXT NOT NULL PRIMARY KEY, TenantId TEXT NOT NULL, ActionId TEXT NOT NULL, Json TEXT NOT NULL, FOREIGN KEY(TenantId,ActionId) REFERENCES GovernedActions(TenantId,ActionId));");
        }
    }

    protected override void Down(MigrationBuilder m) => throw new NotSupportedException("Destructive downgrade is not supported.");
    protected override void BuildTargetModel(ModelBuilder b)
    {
        b.HasAnnotation("ProductVersion", "10.0.12");
        InitialGovernanceModel.ConfigureInitialModel(b);
    }
}


