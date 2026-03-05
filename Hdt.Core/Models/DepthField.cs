namespace Hdt.Core.Models;

public sealed record DepthField
{
    public string Schema { get; init; } = "oan.hopng_depth_field";
    public string SchemaVersion { get; init; } = "0.1.0";
    public string ArtifactId { get; init; } = string.Empty;
    public List<DepthPlane> Planes { get; init; } = [];
}

public sealed record DepthPlane
{
    public string LayerId { get; init; } = string.Empty;
    public double NeutralPlane { get; init; }
    public double MinimumZ { get; init; }
    public double MaximumZ { get; init; }
}
