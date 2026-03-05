namespace Hdt.Core.Models;

public sealed record CrypticReference
{
    public string Id { get; init; } = string.Empty;
    public string PointerUri { get; init; } = string.Empty;
    public string Policy { get; init; } = string.Empty;
    public string? Summary { get; init; }
}
