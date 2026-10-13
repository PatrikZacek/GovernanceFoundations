
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Action;

public sealed record ActionView(
    Guid ActionId,
    ActionState State,
    long Revision,
    ReasonCode Code,
    Guid? ApprovalId,
    DateTimeOffset? ApprovalDeadline,
    string ProposalFingerprint
);

