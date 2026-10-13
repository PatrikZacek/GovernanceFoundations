using System.Text.Json.Serialization;
using Governance.Abstractions.Convertors;
using Governance.Abstractions.Enums;

namespace Governance.Abstractions.Contracts;

/// <summary>A stable, extensible reason identifier.
/// Serialized as a JSON string, never an ordinal.</summary>
[JsonConverter(typeof(ReasonCodeJsonConverter))]
public readonly record struct ReasonCode
{
    private readonly string? value;

    public ReasonCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length is < 1 or > 80 || !value.All(IsIdentifierCharacter))
        {
            throw new ArgumentException(
                "Reason codes must contain 1–80 ASCII letters, digits, dots, hyphens or underscores.", nameof(value));
        }

        this.value = value == nameof(RuntimeReasonCode.Unspecified) ? null : value;
    }

    public string Value => value ?? nameof(RuntimeReasonCode.Unspecified);
    public bool IsSpecified => value is not null;
    public override string ToString() => Value;

    public static implicit operator ReasonCode(RuntimeReasonCode code)
    {
        return !Enum.IsDefined(code)
            ? throw new ArgumentOutOfRangeException(nameof(code))
            : new ReasonCode(code.ToString());
    }

    public static implicit operator ReasonCode(StoreResultCode code)
    {
        return !Enum.IsDefined(code)
            ? throw new ArgumentOutOfRangeException(nameof(code))
            : new ReasonCode(code.ToString());
    }

    private static bool IsIdentifierCharacter(char character)
        => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_';
}
