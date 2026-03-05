using System.Text.Json;

namespace Hdt.Core.Services;

public sealed class ArtifactJsonStore
{
    public void WriteCanonical<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, CanonicalJson.SerializeToUtf8Bytes(value));
    }

    public T Read<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonDefaults.SerializerOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize '{path}'.");
    }

    public JsonDocument ReadDocument(string path) => JsonDocument.Parse(File.ReadAllBytes(path));
}
