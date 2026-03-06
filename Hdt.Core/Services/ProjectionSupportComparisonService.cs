using Hdt.Core.Models;

namespace Hdt.Core.Services;

public sealed class ProjectionSupportComparisonService
{
    public ProjectionSupportComparisonResult Compare(GovernedProjectionResult left, GovernedProjectionResult right)
    {
        var classification = Classify(left.Status, right.Status);
        var signals = BuildSignals(left, right);

        return new ProjectionSupportComparisonResult
        {
            LeftArtifactId = left.ArtifactId,
            RightArtifactId = right.ArtifactId,
            LeftStatus = left.Status,
            RightStatus = right.Status,
            Classification = classification,
            Signals = signals
        };
    }

    private static string Classify(ProjectionFormationStatus left, ProjectionFormationStatus right)
    {
        if (left == right)
        {
            return left switch
            {
                ProjectionFormationStatus.LawfullyFormed => "equivalent-lawful-support",
                ProjectionFormationStatus.StructurallyIncomplete => "equivalent-incomplete-support",
                _ => "equivalent-flattened-support"
            };
        }

        if ((left == ProjectionFormationStatus.LawfullyFormed && right == ProjectionFormationStatus.StructurallyIncomplete)
            || (right == ProjectionFormationStatus.LawfullyFormed && left == ProjectionFormationStatus.StructurallyIncomplete))
        {
            return "formed-vs-incomplete";
        }

        if ((left == ProjectionFormationStatus.LawfullyFormed && right == ProjectionFormationStatus.FlattenedOrUnsupported)
            || (right == ProjectionFormationStatus.LawfullyFormed && left == ProjectionFormationStatus.FlattenedOrUnsupported))
        {
            return "formed-vs-flattened";
        }

        return "incomplete-vs-flattened";
    }

    private static List<string> BuildSignals(GovernedProjectionResult left, GovernedProjectionResult right)
    {
        var signals = new List<string>();

        if (left.Status == ProjectionFormationStatus.LawfullyFormed || right.Status == ProjectionFormationStatus.LawfullyFormed)
        {
            signals.Add("At least one artifact preserves lawful projection provenance.");
        }

        if (left.Status == ProjectionFormationStatus.FlattenedOrUnsupported || right.Status == ProjectionFormationStatus.FlattenedOrUnsupported)
        {
            signals.Add("At least one artifact lacks governed derivation support for Phase 2 projection.");
        }

        if (left.Status == ProjectionFormationStatus.StructurallyIncomplete || right.Status == ProjectionFormationStatus.StructurallyIncomplete)
        {
            signals.Add("At least one artifact declares relational structure but does not satisfy derivation or legibility requirements.");
        }

        return signals;
    }
}
