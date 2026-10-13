namespace Governance.Core.Model;

public sealed record ApprovalSettings(string Capability, TimeSpan Lifetime, bool DisallowSelfApproval = true);

