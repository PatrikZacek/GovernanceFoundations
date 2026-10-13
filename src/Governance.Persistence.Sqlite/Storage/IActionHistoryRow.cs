namespace Governance.Persistence.Sqlite.Storage;

public interface IActionHistoryRow
{
    Guid Id { get; set; }
    string TenantId { get; set; }
    Guid ActionId { get; set; }
    long Sequence { get; set; }
}
