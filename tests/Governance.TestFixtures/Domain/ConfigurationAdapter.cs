using System.Collections.Immutable;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.TestFixtures.Domain;

public sealed class ConfigurationAdapter(ConfigurationTarget target, DemoBehavior behavior) :
    IActionFactsProvider<UpdateConfigurationValue>, IActionExecutor<UpdateConfigurationValue>,
    IActionVerifier<UpdateConfigurationValue>, IActionReconciler<UpdateConfigurationValue>
{
    public async ValueTask<FactsSnapshot> ReadAsync(UpdateConfigurationValue payload, TrustedPrincipal principal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entry = await target.ReadAsync(principal.TenantId, payload.Environment, payload.Key);
        return new(entry?.Version.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing",
            ImmutableDictionary<string, string>.Empty);
    }

    public async Task<ExecutionResult> ExecuteAsync(ExecutionContext<UpdateConfigurationValue> context, CancellationToken ct)
    {
        behavior.RecordDispatch();
        if (behavior.UnknownExecution) return new(Effect.Unknown, RuntimeReasonCode.ExecutionOutcomeUnknown);
        if (behavior.BeforeWrite is { } beforeWrite) await beforeWrite();
        ct.ThrowIfCancellationRequested();
        return await target.ApplyAsync(context.TenantId, context.Payload, context.ExecutionKey)
            ? new(Effect.Applied, RuntimeReasonCode.Ok, context.ExecutionKey)
            : new(Effect.NotApplied, RuntimeReasonCode.TargetVersionConflict);
    }

    public async Task<ExecutionResult> ReconcileAsync(ExecutionContext<UpdateConfigurationValue> context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await target.HasReceiptAsync(context.TenantId, context.ExecutionKey)
            ? new(Effect.Applied, RuntimeReasonCode.Ok, context.ExecutionKey)
            : new(Effect.Unknown, RuntimeReasonCode.ExecutionOutcomeUnknown);
    }

    public async Task<VerificationResult> VerifyAsync(ExecutionContext<UpdateConfigurationValue> context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entry = await target.ReadAsync(context.TenantId, context.Payload.Environment, context.Payload.Key);
        if (entry?.Version > context.ExpectedVersion + 1)
            return new(VerificationOutcome.Inconclusive, ConfigurationReasonCodes.TargetAdvanced);
        if (behavior.VerificationMismatch || entry != new ConfigurationEntry(context.Payload.NewValue, context.ExpectedVersion + 1))
            return new(VerificationOutcome.Mismatch, RuntimeReasonCode.VerificationMismatch);
        return new(VerificationOutcome.Verified, RuntimeReasonCode.Ok, context.ExecutionKey, entry.Version.ToString());
    }
}
