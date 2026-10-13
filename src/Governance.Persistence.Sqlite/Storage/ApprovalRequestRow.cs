namespace Governance.Persistence.Sqlite.Storage;

public sealed class ApprovalRequestRow : IActionHistoryRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public string BindingHash { get; set; } = string.Empty;
    public string Capability { get; set; } = string.Empty;
    public DateTimeOffset Deadline { get; set; }
}
