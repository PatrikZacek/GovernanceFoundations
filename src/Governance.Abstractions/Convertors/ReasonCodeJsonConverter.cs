using System.Text.Json;
using System.Text.Json.Serialization;
using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Convertors;

public sealed class ReasonCodeJsonConverter : JsonConverter<ReasonCode>
{
    public override ReasonCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("A reason code must be a string.");
        }

        try
        {
            return new ReasonCode(reader.GetString()!);
        }
        catch (ArgumentException)
        {
            throw new JsonException("Invalid reason code identifier.");
        }
    }

    public override void Write(Utf8JsonWriter writer, ReasonCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}
