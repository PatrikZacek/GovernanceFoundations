using Governance.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Core;

public static class AuditEventTypes
{
    public static AuditEventType FromCommand(CommandKind command) => command switch
    {
        CommandKind.Evaluate => AuditEventType.Evaluate,
        CommandKind.DecideApproval => AuditEventType.DecideApproval,
        CommandKind.Claim => AuditEventType.Claim,
        CommandKind.RecordOutcome => AuditEventType.RecordOutcome,
        CommandKind.RecordVerification => AuditEventType.RecordVerification,
        CommandKind.Cancel => AuditEventType.Cancel,
        CommandKind.RetryEvaluation => AuditEventType.RetryEvaluation,
        CommandKind.RecoverClaim => AuditEventType.RecoverClaim,
        _ => throw new ArgumentOutOfRangeException(nameof(command))
    };
}
