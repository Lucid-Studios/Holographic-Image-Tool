using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hdt.Core;

public static class CanonicalJson
{
    public static byte[] SerializeToUtf8Bytes<T>(T value)
    {
        var node = JsonSerializer.SerializeToNode(value, JsonDefaults.SerializerOptions)
            ?? throw new InvalidOperationException("Unable to serialize value to canonical JSON.");
        var normalized = Normalize(node);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        normalized.WriteTo(writer, JsonDefaults.SerializerOptions);
        writer.Flush();
        return stream.ToArray();
    }

    public static string Serialize<T>(T value) => System.Text.Encoding.UTF8.GetString(SerializeToUtf8Bytes(value));

    private static JsonNode Normalize(JsonNode node) =>
        node switch
        {
            JsonObject obj => NormalizeObject(obj),
            JsonArray arr => NormalizeArray(arr),
            _ => node.DeepClone()
        };

    private static JsonObject NormalizeObject(JsonObject obj)
    {
        var normalized = new JsonObject();
        foreach (var property in obj.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            normalized[property.Key] = property.Value is null ? null : Normalize(property.Value);
        }

        return normalized;
    }

    private static JsonArray NormalizeArray(JsonArray arr)
    {
        var normalized = new JsonArray();
        foreach (var item in arr)
        {
            normalized.Add(item is null ? null : Normalize(item));
        }

        return normalized;
    }
}
