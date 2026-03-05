using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hdt.Tests.TestSupport;

public static class JsonFile
{
    public static void Mutate(string path, Action<JsonObject> mutate)
    {
        var node = JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidOperationException($"Unable to parse JSON file '{path}'.");
        mutate(node);
        File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
