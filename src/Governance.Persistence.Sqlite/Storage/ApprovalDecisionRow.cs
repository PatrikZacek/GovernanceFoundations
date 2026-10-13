using Governance.Abstractions.Contracts;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class ApprovalDecisionRow : IActionHistoryRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public Guid RequestId { get; set; }
    public string ApproverId { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public DateTimeOffset At { get; set; }
    public ReasonCode ReasonCode { get; set; }
    public long Revision { get; set; }
}
