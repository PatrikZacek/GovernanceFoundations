namespace Governance.Persistence.Sqlite.Storage;

public sealed class ExecutionAttemptRow : IActionHistoryRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public string ExecutionKey { get; set; } = string.Empty;
    public DateTimeOffset ClaimedAt { get; set; }
    public string WorkerId { get; set; } = string.Empty;
    public Guid EvaluationId { get; set; }
}
