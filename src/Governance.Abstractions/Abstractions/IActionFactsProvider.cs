using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface IActionFactsProvider<T>
{
    ValueTask<FactsSnapshot> ReadAsync(T payload, TrustedPrincipal principal, CancellationToken ct);
}

