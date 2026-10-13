using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed record ExecutionResult(
    Effect Effect,
    ReasonCode Code,
    string? ReceiptReference = null);
