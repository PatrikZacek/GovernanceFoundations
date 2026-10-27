using System.Collections.Concurrent;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Policy;

namespace Governance.TestFixtures.Domain;

public sealed record UpdateConfigurationValue(string Environment, string Key, string NewValue, long ExpectedVersion);
public sealed record ConfigurationEntry(string Value, long Version);

public static class ConfigurationReasonCodes
{
    public static readonly ReasonCode TargetAdvanced = new("configuration.target-advanced");
}

public sealed class DemoBehavior
{
    private int dispatchCount;
    public int DispatchCount => Volatile.Read(ref dispatchCount);
    public void RecordDispatch() => Interlocked.Increment(ref dispatchCount);
    public bool PolicyThrows { get; set; }
    public bool PolicyTimeout { get; set; }
    public bool UnknownExecution { get; set; }
    public bool VerificationMismatch { get; set; }
    public bool RequireDevelopmentApproval { get; set; }
    public bool DenyAll { get; set; }
    public Func<Task>? BeforeWrite { get; set; }
}

public sealed class DemoAuthorization : IGovernanceAuthorizationService
{
    private readonly ConcurrentDictionary<(string Actor, string Capability), byte> revoked = new();
    public void Revoke(string actor, string capability) => revoked.TryAdd((actor, capability), 0);

    public ValueTask<bool> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var actor = request.Principal.PrincipalId;
        var allowed = request.Capability switch
        {
            "configuration.propose" => actor is "alice" or "service",
            "configuration.approve" => actor is "alice" or "bob",
            "governance.execute" => actor == "worker",
            _ => actor is "alice" or "bob" or "worker" or "service"
        };
        return ValueTask.FromResult(allowed && !revoked.ContainsKey((actor, request.Capability)));
    }
}

public sealed class ConfigurationValidator : IActionValidator<UpdateConfigurationValue>
{
    public ValidationResult<UpdateConfigurationValue> Validate(UpdateConfigurationValue payload)
    {
        var valid = payload.Environment is "development" or "production"
                    && !string.IsNullOrWhiteSpace(payload.Key)
                    && payload.NewValue is "true" or "false" && payload.ExpectedVersion > 0;
        return new(valid, valid ? payload : null, $"{payload.Environment}/{payload.Key}", payload.ExpectedVersion);
    }
}

public sealed class ConfigurationPolicy(DemoBehavior behavior) : IActionPolicy<UpdateConfigurationValue>
{
    public async ValueTask<PolicyResult> EvaluateAsync(PolicyContext<UpdateConfigurationValue> context, CancellationToken ct)
    {
        if (behavior.PolicyThrows) throw new InvalidOperationException("Simulated policy failure.");
        if (behavior.PolicyTimeout) await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        if (behavior.DenyAll || context.Payload.Key.StartsWith("auth.", StringComparison.Ordinal))
            return new(PolicyDecision.Deny, RuntimeReasonCode.PolicyDenied, "Protected configuration.");
        if (context.Payload.Environment == "production" || behavior.RequireDevelopmentApproval)
            return new(PolicyDecision.RequireApproval, RuntimeReasonCode.ApprovalRequired, "Review required.");
        return new(PolicyDecision.Allow, RuntimeReasonCode.Ok, "Development configuration.");
    }
}
