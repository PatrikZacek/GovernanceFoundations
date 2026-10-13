using Governance.Persistence.Sqlite.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Governance.Persistence.Sqlite.Configuration;

internal static class HistoryRowConfiguration
{
    public static void Configure<T>(EntityTypeBuilder<T> builder) where T : class, IActionHistoryRow
    {
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.HasAlternateKey(row => new { row.TenantId, row.ActionId, row.Id });
        builder.HasIndex(row => new { row.TenantId, row.ActionId, row.Sequence }).IsUnique();
        builder.HasOne<ActionRow>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.ActionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
