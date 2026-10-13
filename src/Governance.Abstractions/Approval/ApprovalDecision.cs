
using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Approval;

public sealed record ApprovalDecision(Guid RequestId, string ApproverId, bool Approved, DateTimeOffset At, ReasonCode ReasonCode, long Revision);

