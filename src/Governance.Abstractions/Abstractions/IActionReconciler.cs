using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IActionReconciler<T>
{
    Task<ExecutionResult> ReconcileAsync(ExecutionContext<T> context, CancellationToken cancellationToken);
}

