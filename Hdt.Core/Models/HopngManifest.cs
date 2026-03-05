namespace Hdt.Core.Models;

public sealed record HopngManifest
{
    public string Schema { get; init; } = "oan.hopng_manifest";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; }
    public string ProjectionFile { get; init; } = string.Empty;
    public List<SidecarReference> Sidecars { get; init; } = [];
    public List<ArtifactFileDigest> FileDigests { get; init; } = [];
    public VisibilityPolicy VisibilityPolicy { get; init; } = new();
    public List<string> PhaseReservations { get; init; } = [];
}
