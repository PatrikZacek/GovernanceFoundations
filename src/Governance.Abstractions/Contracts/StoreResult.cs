using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

public sealed record StoreResult(StoreResultCode Code, GovernedAction? Action)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSuccess => Code == StoreResultCode.Ok;
}

