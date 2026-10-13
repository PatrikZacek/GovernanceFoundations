using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class ApprovalRequestRowConfiguration : IEntityTypeConfiguration<ApprovalRequestRow>
{
    public void Configure(EntityTypeBuilder<ApprovalRequestRow> builder)
    {
        builder.ToTable("ApprovalRequests");
        HistoryRowConfiguration.Configure(builder);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.BindingHash).IsRequired();
        builder.Property(row => row.Capability).IsRequired();
    }
}
