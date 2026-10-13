using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class ActionRowConfiguration : IEntityTypeConfiguration<ActionRow>
{
    public void Configure(EntityTypeBuilder<ActionRow> builder)
    {
        builder.ToTable("GovernedActions");
        builder.HasKey(row => new { row.TenantId, row.ActionId });
        builder.Property(row => row.ActionId).ValueGeneratedNever();
        builder.Property(row => row.Revision).IsConcurrencyToken();
        builder.HasIndex(row => new { row.TenantId, row.ProposerId, row.ActionTypeId, row.IdempotencyKey })
            .IsUnique().HasDatabaseName("UX_Actions_Idempotency");
        builder.HasIndex(row => new { row.TenantId, row.ApprovalId })
            .IsUnique().HasDatabaseName("UX_Actions_Approval");
        builder.HasIndex(row => new { row.State, row.TenantId, row.ActionId })
            .HasDatabaseName("IX_Actions_Recovery");
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.ProposerId).IsRequired();
        builder.Property(row => row.ActionTypeId).IsRequired();
        builder.Property(row => row.PayloadJson).IsRequired();
        builder.Property(row => row.PayloadFingerprint).IsRequired();
        builder.Property(row => row.ProposalFingerprint).IsRequired();
        builder.Property(row => row.TargetResourceId).IsRequired();
        builder.Property(row => row.IdempotencyKey).IsRequired();
        builder.Property(row => row.ManifestHash).IsRequired();
        builder.Property(row => row.State).HasConversion<string>();
        builder.Property(row => row.Code).HasConversion<ReasonCodeValueConverter>();
        builder.HasOne<ApprovalRequestRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId, row.ApprovalId })
            .HasPrincipalKey(row => new { row.TenantId, row.ActionId, row.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
