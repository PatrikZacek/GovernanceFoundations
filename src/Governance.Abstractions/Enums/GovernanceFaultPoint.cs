namespace Governance.Abstractions.Enums;

public enum GovernanceFaultPoint
{
    BeforeSubmissionCommit,
    AfterSubmission,
    BeforeTransitionCommit,
    BeforeClaimCommit,
    AfterClaimCommit,
    AfterClaim,
    AfterTargetCommit,
    DuringVerification
}
