using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class ActionRow
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public string ProposerId { get; set; } = string.Empty;
    public bool ProposerIsHuman { get; set; }
    public string? OnBehalfOfPrincipalId { get; set; }
    public string? AgentId { get; set; }
    public string ActionTypeId { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string PayloadFingerprint { get; set; } = string.Empty;
    public string ProposalFingerprint { get; set; } = string.Empty;
    public string TargetResourceId { get; set; } = string.Empty;
    public long ExpectedTargetVersion { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string ManifestHash { get; set; } = string.Empty;
    public ActionState State { get; set; }
    public long Revision { get; set; }
    public ReasonCode Code { get; set; }
    public Guid? ApprovalId { get; set; }
}
