using System.Text;
using System.Text.Json;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Core.Model;

namespace Governance.Core.Runtime;

internal sealed class ProposalService(GovernanceRuntime runtime, PolicyEvaluationService evaluation)
{
    public async Task<GovernanceResult> SubmitAsync<T>(T payload, SubmissionOptions submission,
        CancellationToken cancellationToken = default)
    {
        var definition = runtime.Model.Actions.FirstOrDefault(definition => definition.PayloadType == typeof(T));
        if (definition is null)
        {
            return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.UnknownActionType,
                runtime.CreateCorrelationId(), cancellationToken);
        }

        try
        {
            return await SubmitJsonAsync(definition.Id, definition.Schema,
                JsonSerializer.Serialize(payload, GovernanceJson.Options), submission, cancellationToken);
        }
        catch (Exception ex) when (ex is not SimulatedCrashException)
        {
            return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.InvalidPayload,
                runtime.CreateCorrelationId(), cancellationToken);
        }
    }

    public async Task<GovernanceResult> SubmitJsonAsync(string actionTypeId, int schemaVersion, string json,
        SubmissionOptions submission, CancellationToken cancellationToken = default)
    {
        var correlationId = runtime.CreateCorrelationId();
        GovernedAction? action = null;
        try
        {
            var definition = runtime.Model.Find(actionTypeId, schemaVersion);
            if (definition is null)
            {
                return await runtime.RecordIngressFailureAsync(
                    runtime.Model.Actions.Any(definition => definition.Id == actionTypeId)
                        ? RuntimeReasonCode.UnsupportedSchema
                        : RuntimeReasonCode.UnknownActionType, correlationId, cancellationToken);
            }

            var proposer = runtime.Principal.Current;
            if (!await runtime.AuthorizeAsync(proposer, GovernanceOperation.Propose, "*", definition.Capability, null,
                    cancellationToken))
            {
                return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.Unauthorized, correlationId,
                    cancellationToken);
            }

            if (Encoding.UTF8.GetByteCount(json) > runtime.Options.PayloadLimitBytes ||
                string.IsNullOrWhiteSpace(submission.IdempotencyKey) || submission.IdempotencyKey.Length > 200 ||
                submission.ExpiresAt <= runtime.UtcNow)
            {
                return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.InvalidPayload, correlationId,
                    cancellationToken);
            }

            NormalizedPayload normalized;
            try
            {
                normalized = definition.Normalize(json, runtime.Services);
            }
            catch (Exception ex) when (ex is not SimulatedCrashException)
            {
                return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.InvalidPayload, correlationId,
                    cancellationToken);
            }

            if (!normalized.Valid || Encoding.UTF8.GetByteCount(normalized.Json) > runtime.Options.PayloadLimitBytes)
            {
                return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.InvalidPayload, correlationId,
                    cancellationToken);
            }

            var payloadFingerprint = GovernanceJson.Hash(normalized.Json);
            var proposal = GovernanceJson.Hash(JsonSerializer.Serialize(new
            {
                definition.Id,
                definition.Schema,
                proposer.TenantId,
                proposer.PrincipalId,
                proposer.OnBehalfOfPrincipalId,
                normalized.ResourceId,
                normalized.ExpectedVersion,
                ExpiresAt = submission.ExpiresAt.ToUniversalTime(),
                payloadFingerprint
            }));
            action = new(Guid.NewGuid(), proposer.TenantId, definition.Id, definition.Schema, normalized.Json,
                payloadFingerprint, proposal, proposer, normalized.ResourceId, normalized.ExpectedVersion,
                submission.IdempotencyKey, runtime.UtcNow, submission.ExpiresAt.ToUniversalTime(),
                runtime.Model.ManifestHash);
            if (!await runtime.IsProposerAuthorizedAsync(action, definition, cancellationToken))
            {
                return await runtime.RecordIngressFailureAsync(RuntimeReasonCode.Unauthorized, correlationId,
                    cancellationToken);
            }

            var inserted = await runtime.Store.InsertProposalAsync(action, correlationId, cancellationToken);
            action = inserted.Action;
            if (inserted.Code != StoreResultCode.Ok)
            {
                return runtime.CreateResult(action, correlationId, inserted.Code);
            }

            runtime.Faults.Hit(GovernanceFaultPoint.AfterSubmission);
            if (action!.State == ActionState.Submitted)
            {
                action = (await evaluation.EvaluateAsync(action, definition, EvaluationPhase.Submission, correlationId,
                    cancellationToken)).Action ?? action;
            }

            return runtime.CreateResult(action, correlationId);
        }
        catch (SimulatedCrashException)
        {
            throw;
        }
        catch
        {
            return runtime.CreateResult(action, correlationId, RuntimeReasonCode.PersistenceUnavailable);
        }
    }

    public async Task<PreviewResult> PreviewAsync<T>(T payload, CancellationToken cancellationToken = default)
    {
        var correlationId = runtime.CreateCorrelationId();
        var definition = runtime.Model.Actions.FirstOrDefault(definition => definition.PayloadType == typeof(T));
        if (definition is null)
        {
            return new(RuntimeReasonCode.UnknownActionType, correlationId, null);
        }

        try
        {
            var json = JsonSerializer.Serialize(payload, GovernanceJson.Options);
            if (Encoding.UTF8.GetByteCount(json) > runtime.Options.PayloadLimitBytes)
            {
                return new(RuntimeReasonCode.InvalidPayload, correlationId, null);
            }

            var normalized = definition.Normalize(json, runtime.Services);
            if (!normalized.Valid)
            {
                return new(RuntimeReasonCode.InvalidPayload, correlationId, null);
            }

            var proposer = runtime.Principal.Current;
            var action = new GovernedAction(Guid.Empty, proposer.TenantId, definition.Id, definition.Schema,
                normalized.Json, "", "", proposer, normalized.ResourceId, normalized.ExpectedVersion, "preview",
                runtime.UtcNow, runtime.UtcNow.AddHours(1), runtime.Model.ManifestHash);
            if (!await runtime.IsProposerAuthorizedAsync(action, definition, cancellationToken))
            {
                return new(RuntimeReasonCode.Unauthorized, correlationId, null);
            }

            var policyEvaluation = await definition.EvaluateAsync(action, EvaluationPhase.Preview, runtime.UtcNow,
                runtime.Services, runtime.Options.PolicyTimeout, cancellationToken);
            return new(new ReasonCode(policyEvaluation.Decision.ToString()), correlationId, policyEvaluation);
        }
        catch
        {
            return new(RuntimeReasonCode.InvalidPayload, correlationId, null);
        }
    }
}
