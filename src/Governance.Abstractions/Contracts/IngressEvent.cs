namespace Governance.Abstractions.Contracts;

public sealed record IngressEvent(
    Guid Id,
    string TenantId,
    DateTimeOffset At,
    string CorrelationId,
    ReasonCode Code);

