namespace Governance.Abstractions.Contracts;

public sealed record ExecutionContext<T>(
    T Payload,
    string TenantId,
    Guid ActionId,
    string ExecutionKey,
    string PayloadFingerprint,
    string ResourceId,
    long ExpectedVersion );
