using System.Collections.Immutable;
using System.Text.Json;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Abstractions.Policy;
using Microsoft.Extensions.DependencyInjection;

namespace Governance.Core.Model;

internal sealed class ActionDefinition<T>(
    string id,
    int schema,
    string capability,
    Type validator,
    Type facts,
    Type executor,
    Type verifier,
    Type? reconciler,
    string adapterVersion,
    ApprovalSettings? approval,
    ImmutableArray<PolicyRegistration> policies ) : IActionDefinition
{
    public string Id => id;
    public int Schema => schema;
    public Type PayloadType => typeof(T);
    public string Capability => capability;
    public ApprovalSettings? Approval => approval;
    public ImmutableArray<PolicyRegistration> Policies => policies;

    public string Manifest => JsonSerializer.Serialize(new
    {
        id, schema, capability, validator = validator.FullName, facts = facts.FullName, executor = executor.FullName,
        verifier = verifier.FullName, reconciler = reconciler?.FullName, adapterVersion, approval,
        policies = policies.Select(p => new { p.Id, p.Version, type = p.Implementation.FullName, p.MayRequireApproval })
    });

    public void ValidateServices(IServiceProvider serviceProvider)
    {
        foreach (var t in new[]
                 {
                     validator,
                     facts,
                     executor,
                     verifier
                 }.Concat(policies.Select(p => p.Implementation)))
        {
            _ = serviceProvider.GetRequiredService(t);
        }

        if (reconciler is not null)
        {
            _ = serviceProvider.GetRequiredService(reconciler);
        }
    }

    private static T Payload(string json)
    {
        return JsonSerializer.Deserialize<T>(json, GovernanceJson.Options) ?? throw new JsonException();
    }

    public NormalizedPayload Normalize(string json, IServiceProvider serviceProvider)
    {
        using var doc = JsonDocument.Parse(json);
        GovernanceJson.RejectDuplicates(doc.RootElement);
        var result = ((IActionValidator<T>)serviceProvider.GetRequiredService(validator)).Validate(Payload(json));
        return result is { Valid: true, Payload: not null }
            ? new NormalizedPayload(true, GovernanceJson.Canonical(result.Payload, typeof(T)), result.ResourceId,
                result.ExpectedVersion)
            : new NormalizedPayload(false, string.Empty, string.Empty, 0);
    }

    public async Task<PolicyEvaluation> EvaluateAsync(GovernedAction governedAction, EvaluationPhase phase, DateTimeOffset now,
        IServiceProvider serviceProvider, TimeSpan timeout, CancellationToken ct)
    {
        var items = ImmutableArray.CreateBuilder<PolicyItem>();
        var snapshot = new FactsSnapshot("unavailable", ImmutableDictionary<string, string>.Empty);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(timeout);
        try
        {
            snapshot = await ((IActionFactsProvider<T>)serviceProvider.GetRequiredService(facts))
                .ReadAsync(Payload(governedAction.PayloadJson), governedAction.Proposer, limit.Token).AsTask().WaitAsync(timeout, ct);
            foreach (var policyRegistration in policies)
            {
                var policyResult = await ((IActionPolicy<T>)serviceProvider.GetRequiredService(policyRegistration.Implementation))
                    .EvaluateAsync(new PolicyContext<T>(Payload(governedAction.PayloadJson), governedAction.Proposer, snapshot, now), limit.Token)
                    .AsTask().WaitAsync(timeout, ct);
                if (!Enum.IsDefined(policyResult.Decision))
                {
                    throw new InvalidOperationException();
                }

                items.Add(new PolicyItem(policyRegistration.Id, policyRegistration.Version, policyResult.Decision, policyResult.ReasonCode, policyResult.SafeMessage));
            }
        }
        catch (Exception ex) when (ex is not SimulatedCrashException)
        {
            items.Add(new PolicyItem("runtime", "1", PolicyDecision.Unavailable, RuntimeReasonCode.PolicyUnavailable,
                "Required policy evaluation did not complete."));
        }

        var decision = items.Any(p => p.Decision == PolicyDecision.Unavailable) ? PolicyDecision.Unavailable :
            items.Any(p => p.Decision == PolicyDecision.Deny) ? PolicyDecision.Deny :
            items.Any(p => p.Decision == PolicyDecision.RequireApproval) ? PolicyDecision.RequireApproval :
            PolicyDecision.Allow;
        return new PolicyEvaluation(Guid.NewGuid(), governedAction.TenantId, governedAction.ActionId, phase, now, governedAction.ManifestHash, snapshot,
            items.ToImmutable(), decision);
    }

    private static ExecutionContext<T> Context(GovernedAction governedAction)
    {
        return new ExecutionContext<T>(Payload(governedAction.PayloadJson), governedAction.TenantId, governedAction.ActionId, governedAction.ExecutionKey,
            governedAction.PayloadFingerprint,
            governedAction.TargetResourceId, governedAction.ExpectedTargetVersion);
    }

    public Task<ExecutionResult> ExecuteAsync(GovernedAction governedAction, IServiceProvider serviceProvider, CancellationToken ct)
    {
        return ((IActionExecutor<T>)serviceProvider.GetRequiredService(executor)).ExecuteAsync(Context(governedAction), ct);
    }

    public Task<VerificationResult> VerifyAsync(GovernedAction governedAction, IServiceProvider serviceProvider, CancellationToken ct)
    {
        return ((IActionVerifier<T>)serviceProvider.GetRequiredService(verifier)).VerifyAsync(Context(governedAction), ct);
    }

    public Task<ExecutionResult> ReconcileAsync(GovernedAction governedAction, IServiceProvider serviceProvider, CancellationToken ct)
    {
        return reconciler is null
            ? Task.FromResult(new ExecutionResult(Effect.Unknown, RuntimeReasonCode.ExecutionOutcomeUnknown))
            : ((IActionReconciler<T>)serviceProvider.GetRequiredService(reconciler)).ReconcileAsync(Context(governedAction), ct);
    }
}
