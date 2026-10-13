using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IActionExecutor<T>
{
    Task<ExecutionResult> ExecuteAsync(ExecutionContext<T> context, CancellationToken cancellationToken);
}

