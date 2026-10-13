namespace Governance.Abstractions.Contracts;

public sealed record SubmissionOptions(string IdempotencyKey, DateTimeOffset ExpiresAt);

