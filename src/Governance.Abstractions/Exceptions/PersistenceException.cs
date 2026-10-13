
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Exceptions;

public sealed class PersistenceException()
    : Exception(nameof(RuntimeReasonCode.PersistenceUnavailable));

