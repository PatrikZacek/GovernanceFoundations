using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class VerificationRow : IActionHistoryRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public Guid AttemptId { get; set; }
    public DateTimeOffset At { get; set; }
    public VerificationOutcome Outcome { get; set; }
    public ReasonCode Code { get; set; }
    public string? EvidenceReference { get; set; }
    public string? SourceVersion { get; set; }
}
