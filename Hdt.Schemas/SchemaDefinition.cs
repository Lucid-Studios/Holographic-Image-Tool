namespace Hdt.Schemas;

public sealed record SchemaDefinition(
    string LogicalName,
    string SchemaVersion,
    string ResourceName,
    bool Implemented,
    int Phase,
    IReadOnlyList<string> RequiredProperties);
