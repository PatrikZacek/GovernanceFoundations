using System.Text.Json.Serialization;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Convertors;

namespace Governance.Abstractions.Enums;

/// <summary>Closed outcomes of an atomic store operation; independent of an action's domain reason.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<StoreResultCode>))]
public enum StoreResultCode
{
    Unspecified,
    Ok,
    NotFoundOrUnauthorized,
    IdempotencyConflict,
    ConcurrencyConflict,
    ApprovalExpired,
    ProposalExpired,
    ApprovalRequired,
    PolicyDenied,
    PersistenceUnavailable
}

