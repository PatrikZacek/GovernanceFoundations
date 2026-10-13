using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Exceptions;

public sealed class PersistenceException : Exception
{
    public PersistenceException() : base(nameof(RuntimeReasonCode.PersistenceUnavailable))
    {
    }

    public PersistenceException(Exception innerException)
        : base(nameof(RuntimeReasonCode.PersistenceUnavailable), innerException)
    {
    }
}
