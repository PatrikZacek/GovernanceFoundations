using Governance.Abstractions.Contracts;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class IngressAuditRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public DateTimeOffset At { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public ReasonCode Code { get; set; }
}
