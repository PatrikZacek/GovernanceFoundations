using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Abstractions;

public interface ITrustedPrincipalAccessor
{
    TrustedPrincipal Current
    {
        get;
    }
}

