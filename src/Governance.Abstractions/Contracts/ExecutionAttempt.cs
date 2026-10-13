namespace Governance.Abstractions.Contracts;

public sealed record ExecutionAttempt(
    Guid Id,
    string ExecutionKey,
    DateTimeOffset ClaimedAt,
    string WorkerId,
    Guid EvaluationId,
    ExecutionResult? Result = null);
