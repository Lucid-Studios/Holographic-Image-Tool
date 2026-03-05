namespace Hdt.Core.Models;

public sealed record VisibilityPolicy
{
    public bool PrimeSafeProjection { get; init; } = true;
    public bool MachineReadableLayersAllowed { get; init; } = true;
    public bool CrypticPointersAllowed { get; init; } = true;
    public List<string> ApprovedMetadataFields { get; init; } =
    [
        "artifactId",
        "displayName",
        "createdUtc",
        "projectionFile"
    ];
    public List<CrypticReference> CrypticReferences { get; init; } = [];
}
