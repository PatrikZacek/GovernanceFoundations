using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class IngressAuditRowConfiguration : IEntityTypeConfiguration<IngressAuditRow>
{
    public void Configure(EntityTypeBuilder<IngressAuditRow> builder)
    {
        builder.ToTable("IngressAuditEvents");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.CorrelationId).IsRequired();
        builder.Property(row => row.Code).HasConversion<ReasonCodeValueConverter>();
    }
}
