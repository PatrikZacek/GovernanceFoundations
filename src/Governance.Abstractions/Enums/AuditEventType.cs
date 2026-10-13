using System.Text.Json.Serialization;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Convertors;

namespace Governance.Abstractions.Enums;

[JsonConverter(typeof(StrictStringEnumConverter<AuditEventType>))]
public enum AuditEventType
{
    ProposalSubmitted,
    Evaluate,
    DecideApproval,
    Claim,
    RecordOutcome,
    RecordVerification,
    Cancel,
    RetryEvaluation,
    RecoverClaim
}

