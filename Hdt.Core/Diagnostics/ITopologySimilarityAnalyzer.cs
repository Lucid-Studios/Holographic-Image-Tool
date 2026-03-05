namespace Hdt.Core.Diagnostics;

public interface ITopologySimilarityAnalyzer
{
    double Compare(string leftArtifactId, string rightArtifactId);
}
