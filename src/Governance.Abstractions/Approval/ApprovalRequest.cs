
namespace Governance.Abstractions.Approval;

public sealed record ApprovalRequest(Guid Id, string BindingHash, string Capability, DateTimeOffset Deadline);

