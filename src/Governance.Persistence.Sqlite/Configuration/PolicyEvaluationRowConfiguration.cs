using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Configuration;

internal sealed class PolicyEvaluationRowConfiguration : IEntityTypeConfiguration<PolicyEvaluationRow>
{
    public void Configure(EntityTypeBuilder<PolicyEvaluationRow> builder)
    {
        builder.ToTable("PolicyEvaluations");
        HistoryRowConfiguration.Configure(builder);
        builder.Property(row => row.TenantId).IsRequired();
        builder.Property(row => row.Phase).HasConversion<string>();
        builder.Property(row => row.ManifestHash).IsRequired();
        builder.Property(row => row.FactsVersion).IsRequired();
        builder.Property(row => row.FactsValuesJson).IsRequired();
        builder.Property(row => row.Decision).HasConversion<string>();
    }
}
