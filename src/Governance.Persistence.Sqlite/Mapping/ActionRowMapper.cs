using Governance.Abstractions.Contracts;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Mapping;

internal static class ActionRowMapper
{
    public static ActionRow Create(GovernedAction action) => new()
    {
        TenantId = action.TenantId,
        ActionId = action.ActionId,
        ProposerId = action.Proposer.PrincipalId,
        ProposerIsHuman = action.Proposer.IsHuman,
        OnBehalfOfPrincipalId = action.Proposer.OnBehalfOfPrincipalId,
        AgentId = action.Proposer.AgentId,
        ActionTypeId = action.ActionTypeId,
        SchemaVersion = action.SchemaVersion,
        PayloadJson = action.PayloadJson,
        PayloadFingerprint = action.PayloadFingerprint,
        ProposalFingerprint = action.ProposalFingerprint,
        TargetResourceId = action.TargetResourceId,
        ExpectedTargetVersion = action.ExpectedTargetVersion,
        IdempotencyKey = action.IdempotencyKey,
        SubmittedAt = action.SubmittedAt,
        ExpiresAt = action.ExpiresAt,
        ManifestHash = action.ManifestHash,
        State = action.State,
        Revision = action.Revision,
        Code = action.Code,
        ApprovalId = action.Approval?.Id
    };

    public static void UpdateLifecycle(ActionRow row, GovernedAction action)
    {
        row.State = action.State;
        row.Revision = action.Revision;
        row.Code = action.Code;
        row.ApprovalId = action.Approval?.Id;
    }
}
