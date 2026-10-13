using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed record AuditEvent(
    Guid Id,
    string TenantId,
    Guid ActionId,
    long Sequence,
    AuditEventType Type,
    DateTimeOffset At,
    string ActorId,
    string CorrelationId,
    ReasonCode Code)
{
    public string SafeMetadataJson
    {
        get;
        init;
    } = "{}";
}
