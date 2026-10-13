using System.Text.Json.Serialization;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Convertors;

namespace Governance.Abstractions.Enums;

/// <summary>Reasons owned by the runtime. Applications define their own <see cref="ReasonCode"/> values.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<RuntimeReasonCode>))]
public enum RuntimeReasonCode
{
    Unspecified,
    Ok,
    Submitted,
    Ready,
    Cancelled,
    UnknownActionType,
    UnsupportedSchema,
    InvalidPayload,
    Unauthorized,
    NotFoundOrUnauthorized,
    PolicyDenied,
    ApprovalRequired,
    ApprovalExpired,
    ProposalExpired,
    IdempotencyConflict,
    ConcurrencyConflict,
    ProposalSuperseded,
    SelfApprovalForbidden,
    PolicyUnavailable,
    PersistenceUnavailable,
    ExecutionOutcomeUnknown,
    VerificationUnavailable,
    VerificationMismatch,
    TargetVersionConflict,
    HumanApproved,
    HumanRejected,
    ExecutionStarted,
    EvaluationRetry,
    RecoveryEvaluationRetry,
    AdapterError
}
