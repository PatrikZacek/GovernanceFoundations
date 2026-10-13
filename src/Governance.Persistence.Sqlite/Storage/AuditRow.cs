using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class AuditRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public long Sequence { get; set; }
    public AuditEventType Type { get; set; }
    public DateTimeOffset At { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public ReasonCode Code { get; set; }
    public string SafeMetadataJson { get; set; } = string.Empty;
}
