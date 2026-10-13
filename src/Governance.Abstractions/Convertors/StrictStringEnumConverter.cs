using System.Text.Json.Serialization;

namespace Governance.Abstractions.Convertors;

/// <summary>Closed protocol enums accept names only, without ordinal fallbacks.</summary>
public sealed class StrictStringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
