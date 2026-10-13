using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Abstractions.Exceptions;

namespace Governance.Testing.Domain;

public sealed class FaultInjector : IGovernanceFaults
{
    private readonly Dictionary<GovernanceFaultPoint, Exception> armed = [];
    public void Arm(GovernanceFaultPoint point, Exception? error = null)
    {
        lock (armed)
        {
            armed[point] = error ?? new SimulatedCrashException();
        }
    }

    public void Hit(GovernanceFaultPoint point)
    {
        lock (armed)
        {
            if (armed.Remove(point, out var error))
            {
                throw error;
            }
        }
    }
}

