using System.Text.Json;
using Governance.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Core.Runtime;

/// <summary>Fixed allowlist for public history. Payloads, facts values and receipts are excluded.</summary>
public static class GovernanceAuditProjection
{
    public static string Project(GovernedAction action, AuditEventType eventType)
    {
        if (eventType != AuditEventType.Evaluate || action.LastEvaluation is null)
        {
            return "{}";
        }

        var evaluation = action.LastEvaluation;
        return JsonSerializer.Serialize(new
        {
            evaluation.Id,
            evaluation.Phase,
            evaluation.ManifestHash,
            FactsVersion = evaluation.Facts.Version,
            Policies = evaluation.Results.Select(policy => new
            {
                policy.PolicyId,
                policy.PolicyVersion,
                Decision = policy.Decision.ToString(),
                policy.ReasonCode,
                policy.SafeMessage
            })
        });
    }
}

