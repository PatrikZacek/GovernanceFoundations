using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Migrations;

internal static class InitialGovernanceModel
{
    // Frozen initial schema: never delegate snapshots to the current runtime model.
    internal static void ConfigureInitialModel(ModelBuilder b)
    {
        var a = b.Entity("Governance.Persistence.Sqlite.Storage.ActionRow");
        a.ToTable("GovernedActions");
        a.Property<string>("TenantId").HasColumnType("TEXT");
        a.Property<Guid>("ActionId").HasColumnType("TEXT").ValueGeneratedNever();
        a.HasKey("TenantId", "ActionId");
        a.Property<string>("ProposerId").IsRequired().HasColumnType("TEXT");
        a.Property<string>("ActionTypeId").IsRequired().HasColumnType("TEXT");
        a.Property<string>("IdempotencyKey").IsRequired().HasColumnType("TEXT");
        a.Property<long>("Revision").HasColumnType("INTEGER");
        a.Property<Guid?>("ApprovalId").HasColumnType("TEXT");
        a.Property<string>("Json").IsRequired().HasColumnType("TEXT");
        a.HasIndex("TenantId", "ProposerId", "ActionTypeId", "IdempotencyKey").IsUnique();
        a.HasIndex("TenantId", "ApprovalId").IsUnique();
        a.Property<long>("Revision").IsConcurrencyToken();
        var h = b.Entity("Governance.Persistence.Sqlite.Storage.AuditRow");
        h.ToTable("AuditEvents");
        h.Property<Guid>("Id").HasColumnType("TEXT").ValueGeneratedNever();
        h.HasKey("Id");
        h.Property<string>("TenantId").IsRequired().HasColumnType("TEXT");
        h.Property<Guid>("ActionId").HasColumnType("TEXT");
        h.Property<long>("Sequence").HasColumnType("INTEGER");
        h.Property<string>("Json").IsRequired().HasColumnType("TEXT");
        h.HasIndex("TenantId", "ActionId", "Sequence").IsUnique();
        h.HasOne("Governance.Persistence.Sqlite.Storage.ActionRow", null).WithMany().HasForeignKey("TenantId", "ActionId").OnDelete(DeleteBehavior.Restrict);
    }
}
