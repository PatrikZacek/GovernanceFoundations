using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed record ValidationResult<T>(
    bool Valid,
    T? Payload,
    string ResourceId,
    long ExpectedVersion,
    ReasonCode Code = default)
{
    public ReasonCode Code { get; init; } = Code.IsSpecified ? Code : RuntimeReasonCode.InvalidPayload;
}

