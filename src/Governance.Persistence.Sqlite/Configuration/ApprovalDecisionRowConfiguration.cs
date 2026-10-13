using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class ApprovalDecisionRowConfiguration : IEntityTypeConfiguration<ApprovalDecisionRow>
{
    public void Configure(EntityTypeBuilder<ApprovalDecisionRow> builder)
    {
        builder.ToTable("ApprovalDecisions");
        HistoryRowConfiguration.Configure(builder);
        builder.HasOne<ApprovalRequestRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId, row.RequestId })
            .HasPrincipalKey(row => new { row.TenantId, row.ActionId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.ApproverId).IsRequired();
        builder.Property(row => row.ReasonCode).HasConversion<ReasonCodeValueConverter>();
    }
}
