using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class AuditRowConfiguration : IEntityTypeConfiguration<AuditRow>
{
    public void Configure(EntityTypeBuilder<AuditRow> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.HasIndex(row => new { row.TenantId, row.ActionId, row.Sequence }).IsUnique().HasDatabaseName("UX_Audit_Sequence");
        builder.HasOne<ActionRow>().WithMany().HasForeignKey(row => new { row.TenantId, row.ActionId }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.Type).HasConversion<string>();
        builder.Property(row => row.ActorId).IsRequired();
        builder.Property(row => row.CorrelationId).IsRequired();
        builder.Property(row => row.Code).HasConversion<ReasonCodeValueConverter>();
        builder.Property(row => row.SafeMetadataJson).IsRequired();
    }
}
