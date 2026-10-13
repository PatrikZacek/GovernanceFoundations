namespace Governance.Abstractions.Contracts;

public sealed record VerificationRecord(
    Guid AttemptId,
    DateTimeOffset At,
    VerificationResult Result);

