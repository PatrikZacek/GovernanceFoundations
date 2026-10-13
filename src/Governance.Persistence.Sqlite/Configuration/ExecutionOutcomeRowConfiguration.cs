using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class ExecutionOutcomeRowConfiguration : IEntityTypeConfiguration<ExecutionOutcomeRow>
{
    public void Configure(EntityTypeBuilder<ExecutionOutcomeRow> builder)
    {
        builder.ToTable("ExecutionOutcomes");
        HistoryRowConfiguration.Configure(builder);
        builder.HasOne<ExecutionAttemptRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId, row.AttemptId })
            .HasPrincipalKey(row => new { row.TenantId, row.ActionId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.Effect).HasConversion<string>();
        builder.Property(row => row.Code).HasConversion<ReasonCodeValueConverter>();
    }
}
