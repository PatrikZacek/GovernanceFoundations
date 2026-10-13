using System.Collections.Immutable;
using Governance.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;

namespace Governance.Core.Model;

public interface IActionDefinition
{
    string Id { get; }

    int Schema { get; }

    Type PayloadType { get; }

    string Capability { get; }

    ApprovalSettings? Approval { get; }

    ImmutableArray<PolicyRegistration> Policies { get; }

    string Manifest { get; }

    void ValidateServices(IServiceProvider services);
    NormalizedPayload Normalize(string json, IServiceProvider services);

    Task<PolicyEvaluation> EvaluateAsync(
        GovernedAction action,
        EvaluationPhase phase,
        DateTimeOffset now,
        IServiceProvider services,
        TimeSpan timeout,
        CancellationToken ct
    );

    Task<ExecutionResult> ExecuteAsync(GovernedAction action, IServiceProvider services, CancellationToken ct);
    Task<ExecutionResult> ReconcileAsync(GovernedAction action, IServiceProvider services, CancellationToken ct);
    Task<VerificationResult> VerifyAsync(GovernedAction action, IServiceProvider services, CancellationToken ct);
}
