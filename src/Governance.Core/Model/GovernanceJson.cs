using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Governance.Core.Model;

public static class GovernanceJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false
    };
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string Canonical(object payload, Type type)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload, type, Options));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            Write(doc.RootElement, writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(JsonElement jsonElement, Utf8JsonWriter writer)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(jsonElement, writer);
                break;
            case JsonValueKind.Array:
                WriteArray(jsonElement, writer);
                break;
            default: jsonElement.WriteTo(writer);
                break;
        }
    }

    private static void WriteArray(JsonElement jsonElement, Utf8JsonWriter writer)
    {
        writer.WriteStartArray();
        foreach (var p in jsonElement.EnumerateArray())
        {
            Write(p, writer);
        }

        writer.WriteEndArray();
    }

    private static void WriteObject(JsonElement jsonElement, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        foreach (var p in jsonElement.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            writer.WritePropertyName(p.Name);
            Write(p.Value, writer);
        }

        writer.WriteEndObject();
    }

    public static void RejectDuplicates(JsonElement jsonElement)
    {
        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException();
                }

                RejectDuplicates(property.Value);
            }
        }

        if (jsonElement.ValueKind != JsonValueKind.Array) return;

        foreach (var p in jsonElement.EnumerateArray())
        {
            RejectDuplicates(p);
        }
    }
}

