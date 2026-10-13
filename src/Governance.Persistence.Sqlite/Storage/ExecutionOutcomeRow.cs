using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class ExecutionOutcomeRow : IActionHistoryRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public Guid AttemptId { get; set; }
    public Effect Effect { get; set; }
    public ReasonCode Code { get; set; }
    public string? ReceiptReference { get; set; }
}
