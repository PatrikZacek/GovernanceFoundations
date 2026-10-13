using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed record VerificationResult(
    VerificationOutcome Outcome,
    ReasonCode Code,
    string? EvidenceReference = null,
    string? SourceVersion = null);

