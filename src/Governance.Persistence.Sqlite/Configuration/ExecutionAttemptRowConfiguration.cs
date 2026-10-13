using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class ExecutionAttemptRowConfiguration : IEntityTypeConfiguration<ExecutionAttemptRow>
{
    public void Configure(EntityTypeBuilder<ExecutionAttemptRow> builder)
    {
        builder.ToTable("ExecutionAttempts");
        HistoryRowConfiguration.Configure(builder);
        builder.HasOne<PolicyEvaluationRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId, row.EvaluationId })
            .HasPrincipalKey(row => new { row.TenantId, row.ActionId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.ExecutionKey).IsRequired();
        builder.Property(row => row.WorkerId).IsRequired();
    }
}
