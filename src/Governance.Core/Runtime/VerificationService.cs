using Governance.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;
using Governance.Core.Model;

namespace Governance.Core.Runtime;

internal sealed class VerificationService(GovernanceRuntime runtime)
{
    public async Task<GovernedAction> VerifyAsync(GovernedAction action, IActionDefinition definition, string correlationId, CancellationToken cancellationToken)
    {
        runtime.Faults.Hit(GovernanceFaultPoint.DuringVerification);
        VerificationResult result;
        try
        {
            result = await definition.VerifyAsync(action, runtime.Services, cancellationToken);
        }
        catch (SimulatedCrashException)
        {
            throw;
        }
        catch
        {
            result = new(VerificationOutcome.Inconclusive, RuntimeReasonCode.VerificationUnavailable);
        }

        return (await runtime.Store.ApplyAsync(runtime.CreateCommand(action, CommandKind.RecordVerification, correlationId) with
        {
            Verification = new(action.Attempt!.Id, runtime.UtcNow, result),
            Code = result.Code
        }, cancellationToken)).Action ?? action;
    }
}


