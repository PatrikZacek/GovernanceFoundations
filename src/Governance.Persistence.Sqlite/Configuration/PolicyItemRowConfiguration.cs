using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class PolicyItemRowConfiguration : IEntityTypeConfiguration<PolicyItemRow>
{
    public void Configure(EntityTypeBuilder<PolicyItemRow> builder)
    {
        builder.ToTable("PolicyResults");
        builder.HasKey(row => new { row.EvaluationId, row.Ordinal });
        builder.HasOne<PolicyEvaluationRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId, row.EvaluationId })
            .HasPrincipalKey(row => new { row.TenantId, row.ActionId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.PolicyId).IsRequired();
        builder.Property(row => row.PolicyVersion).IsRequired();
        builder.Property(row => row.Decision).HasConversion<string>();
        builder.Property(row => row.ReasonCode).HasConversion<ReasonCodeValueConverter>();
        builder.Property(row => row.SafeMessage).IsRequired();
    }
}
