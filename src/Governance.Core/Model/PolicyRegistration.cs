namespace Governance.Core.Model;

public sealed record PolicyRegistration(string Id, string Version, Type Implementation, bool MayRequireApproval);

