using Governance.Persistence.Sqlite.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class RuntimeDb(DbContextOptions<RuntimeDb> options) : DbContext(options)
{
    public DbSet<ActionRow> Actions => Set<ActionRow>();
    public DbSet<AuditRow> Audit => Set<AuditRow>();
    public DbSet<IngressAuditRow> IngressAudit => Set<IngressAuditRow>();
    public DbSet<PolicyEvaluationRow> PolicyEvaluations => Set<PolicyEvaluationRow>();
    public DbSet<PolicyItemRow> PolicyResults => Set<PolicyItemRow>();
    public DbSet<ApprovalRequestRow> ApprovalRequests => Set<ApprovalRequestRow>();
    public DbSet<ApprovalDecisionRow> ApprovalDecisions => Set<ApprovalDecisionRow>();
    public DbSet<ExecutionAttemptRow> ExecutionAttempts => Set<ExecutionAttemptRow>();
    public DbSet<ExecutionOutcomeRow> ExecutionOutcomes => Set<ExecutionOutcomeRow>();
    public DbSet<VerificationRow> Verifications => Set<VerificationRow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new ActionRowConfiguration());
        builder.ApplyConfiguration(new AuditRowConfiguration());
        builder.ApplyConfiguration(new IngressAuditRowConfiguration());
        builder.ApplyConfiguration(new PolicyEvaluationRowConfiguration());
        builder.ApplyConfiguration(new ApprovalRequestRowConfiguration());
        builder.ApplyConfiguration(new ApprovalDecisionRowConfiguration());
        builder.ApplyConfiguration(new ExecutionAttemptRowConfiguration());
        builder.ApplyConfiguration(new ExecutionOutcomeRowConfiguration());
        builder.ApplyConfiguration(new VerificationRowConfiguration());
        builder.ApplyConfiguration(new PolicyItemRowConfiguration());
    }
}
