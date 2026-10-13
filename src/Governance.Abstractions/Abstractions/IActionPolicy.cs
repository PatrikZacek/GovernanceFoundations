
using Governance.Abstractions.Policy;

namespace Governance.Abstractions.Abstractions;

public interface IActionPolicy<T>
{
    ValueTask<PolicyResult> EvaluateAsync(PolicyContext<T> context, CancellationToken cancellationToken);
}

