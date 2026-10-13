using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed class NoGovernanceFaults : IGovernanceFaults
{
    public void Hit(GovernanceFaultPoint point)
    {
    }
}

