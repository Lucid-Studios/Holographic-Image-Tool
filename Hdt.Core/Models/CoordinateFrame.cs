namespace Hdt.Core.Models;

public sealed record CoordinateFrame
{
    public string XAxis { get; init; } = "x";
    public string YAxis { get; init; } = "y";
    public string ZAxis { get; init; } = "z";
    public string Units { get; init; } = "arbitrary";
}
