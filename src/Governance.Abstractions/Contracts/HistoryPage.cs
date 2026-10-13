using System.Collections.Immutable;

namespace Governance.Abstractions.Contracts;

public sealed record HistoryPage(
    ReasonCode Code,
    string CorrelationId,
    ImmutableArray<AuditEvent> Events,
    long? NextCursor);

