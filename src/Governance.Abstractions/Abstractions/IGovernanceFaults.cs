using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Abstractions;

public interface IGovernanceFaults
{
    void Hit(GovernanceFaultPoint point);
}

