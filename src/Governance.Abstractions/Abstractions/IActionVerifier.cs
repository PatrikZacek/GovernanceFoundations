using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IActionVerifier<T>
{
    Task<VerificationResult> VerifyAsync(ExecutionContext<T> context, CancellationToken cancellationToken);
}

