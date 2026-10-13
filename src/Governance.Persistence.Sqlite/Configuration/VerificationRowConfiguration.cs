using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class VerificationRowConfiguration : IEntityTypeConfiguration<VerificationRow>
{
    public void Configure(EntityTypeBuilder<VerificationRow> builder)
    {
        builder.ToTable("VerificationRecords");
        HistoryRowConfiguration.Configure(builder);
        builder.HasOne<ExecutionAttemptRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId, row.AttemptId })
            .HasPrincipalKey(row => new { row.TenantId, row.ActionId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.Outcome).HasConversion<string>();
        builder.Property(row => row.Code).HasConversion<ReasonCodeValueConverter>();
    }
}
