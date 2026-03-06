using Hdt.Core.Models;
using Hdt.Core.Validation;

namespace Hdt.Core.Services;

public sealed class GovernedProjectionDerivationService
{
    private static readonly HashSet<ValidationErrorCode> TrustFailureCodes =
    [
        ValidationErrorCode.DigestMismatch,
        ValidationErrorCode.HashMismatch,
        ValidationErrorCode.SignatureMismatch
    ];

    private static readonly HashSet<ValidationErrorCode> RelationalFailureCodes =
    [
        ValidationErrorCode.InvalidUniverseLayer,
        ValidationErrorCode.InvalidGluingManifest,
        ValidationErrorCode.InvalidProjectionRules,
        ValidationErrorCode.InvalidLegibilityProfile,
        ValidationErrorCode.MissingSidecar
    ];

    private readonly HopngArtifactLoader _loader = new();
    private readonly HopngArtifactValidator _validator = new();

    public GovernedProjectionResult Derive(string path)
    {
        var artifact = _loader.Load(path);
        var validation = _validator.Validate(path);
        return Derive(artifact, validation);
    }

    public GovernedProjectionResult Derive(LoadedHopngArtifact artifact, ValidationResult validationResult)
    {
        var result = new GovernedProjectionResult
        {
            ArtifactId = artifact.Manifest.ArtifactId,
            ValidationIssues = [.. validationResult.Errors]
        };

        var hasRelationalArtifacts = artifact.UniverseLayerSet is not null
            || artifact.GluingManifest is not null
            || artifact.ProjectionRules is not null
            || artifact.LegibilityProfile is not null;

        if (!hasRelationalArtifacts)
        {
            return result with
            {
                Status = ProjectionFormationStatus.FlattenedOrUnsupported,
                LegibilitySatisfied = false,
                ProjectionIntegritySatisfied = false,
                Issues =
                [
                    "Artifact does not declare the Phase 2 relational sidecars required for governed projection derivation."
                ]
            };
        }

        if (validationResult.Errors.Any(error => TrustFailureCodes.Contains(error.Code)))
        {
            return result with
            {
                Status = ProjectionFormationStatus.FlattenedOrUnsupported,
                LegibilitySatisfied = false,
                ProjectionIntegritySatisfied = false,
                Issues =
                [
                    "Trust validation failed, so governed projection support cannot be treated as lawful."
                ]
            };
        }

        if (artifact.UniverseLayerSet is null || artifact.ProjectionRules is null)
        {
            return result with
            {
                Status = ProjectionFormationStatus.FlattenedOrUnsupported,
                LegibilitySatisfied = false,
                ProjectionIntegritySatisfied = false,
                Issues =
                [
                    "Projection derivation requires both universe layers and projection rules."
                ]
            };
        }

        var orderedRules = artifact.ProjectionRules.Rules
            .OrderBy(rule => rule.Precedence)
            .ThenBy(rule => rule.RuleId, StringComparer.Ordinal)
            .ToList();
        var participatingUniverses = orderedRules
            .Select(rule => rule.SourceUniverseId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var participatingRelations = ResolveParticipatingRelations(artifact, participatingUniverses);
        var trace = BuildTrace(artifact, orderedRules, participatingUniverses);
        var issues = new List<string>();
        var legibilitySatisfied = EvaluateLegibility(artifact, participatingUniverses, participatingRelations, orderedRules, issues, out var projectionIntegritySatisfied);

        if (!orderedRules.Any())
        {
            issues.Add("No projection rules were available for derivation.");
        }

        if (artifact.GluingManifest is not null)
        {
            foreach (var relation in artifact.GluingManifest.Relations.Where(relation => relation.RequiredForFormation))
            {
                if (!participatingRelations.Contains(relation.RelationId, StringComparer.Ordinal))
                {
                    issues.Add($"Required formation relation '{relation.RelationId}' does not participate in the governed projection trace.");
                }
            }
        }

        if (validationResult.Errors.Any(error => RelationalFailureCodes.Contains(error.Code)))
        {
            issues.Add("Relational validation failed, so the projection trace remains structurally incomplete.");
        }

        var status = DetermineStatus(hasRelationalArtifacts, validationResult, issues, legibilitySatisfied);

        return result with
        {
            Status = status,
            LegibilitySatisfied = legibilitySatisfied,
            ProjectionIntegritySatisfied = projectionIntegritySatisfied,
            ParticipatingUniverses = participatingUniverses,
            ParticipatingRelations = participatingRelations,
            RuleTrace = trace,
            Issues = issues
        };
    }

    private static ProjectionFormationStatus DetermineStatus(
        bool hasRelationalArtifacts,
        ValidationResult validationResult,
        IReadOnlyCollection<string> issues,
        bool legibilitySatisfied)
    {
        if (!hasRelationalArtifacts || validationResult.Errors.Any(error => TrustFailureCodes.Contains(error.Code)))
        {
            return ProjectionFormationStatus.FlattenedOrUnsupported;
        }

        if (validationResult.Errors.Any(error => RelationalFailureCodes.Contains(error.Code)) || !legibilitySatisfied || issues.Count > 0)
        {
            return ProjectionFormationStatus.StructurallyIncomplete;
        }

        return ProjectionFormationStatus.LawfullyFormed;
    }

    private static List<string> ResolveParticipatingRelations(LoadedHopngArtifact artifact, IReadOnlyCollection<string> participatingUniverses)
    {
        if (artifact.GluingManifest is null)
        {
            return [];
        }

        return artifact.GluingManifest.Relations
            .Where(relation =>
                participatingUniverses.Contains(relation.SourceUniverseId, StringComparer.Ordinal)
                && participatingUniverses.Contains(relation.TargetUniverseId, StringComparer.Ordinal))
            .Select(relation => relation.RelationId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<ProjectionRuleApplicationTrace> BuildTrace(
        LoadedHopngArtifact artifact,
        IReadOnlyList<ProjectionRule> orderedRules,
        IReadOnlyCollection<string> participatingUniverses)
    {
        var relations = artifact.GluingManifest?.Relations ?? [];

        return orderedRules
            .Select(rule => new ProjectionRuleApplicationTrace
            {
                RuleId = rule.RuleId,
                SourceUniverseId = rule.SourceUniverseId,
                TargetProjectionRole = rule.TargetProjectionRole,
                MappingType = rule.MappingType,
                Precedence = rule.Precedence,
                SupportingRelationIds = relations
                    .Where(relation =>
                        string.Equals(relation.SourceUniverseId, rule.SourceUniverseId, StringComparison.Ordinal)
                        || string.Equals(relation.TargetUniverseId, rule.SourceUniverseId, StringComparison.Ordinal))
                    .Where(relation =>
                        participatingUniverses.Contains(relation.SourceUniverseId, StringComparer.Ordinal)
                        && participatingUniverses.Contains(relation.TargetUniverseId, StringComparer.Ordinal))
                    .Select(relation => relation.RelationId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList(),
                Applied = true,
                Outcome = "governed"
            })
            .ToList();
    }

    private static bool EvaluateLegibility(
        LoadedHopngArtifact artifact,
        IReadOnlyCollection<string> participatingUniverses,
        IReadOnlyCollection<string> participatingRelations,
        IReadOnlyCollection<ProjectionRule> orderedRules,
        List<string> issues,
        out bool projectionIntegritySatisfied)
    {
        projectionIntegritySatisfied = true;
        if (artifact.LegibilityProfile is null)
        {
            return true;
        }

        var legibilitySatisfied = true;

        foreach (var requiredUniverse in artifact.LegibilityProfile.RequiredUniverses)
        {
            if (!participatingUniverses.Contains(requiredUniverse, StringComparer.Ordinal))
            {
                issues.Add($"Legibility requires universe '{requiredUniverse}' to participate in derivation.");
                legibilitySatisfied = false;
                projectionIntegritySatisfied = false;
            }
        }

        foreach (var requiredRelation in artifact.LegibilityProfile.RequiredRelations)
        {
            if (!participatingRelations.Contains(requiredRelation, StringComparer.Ordinal))
            {
                issues.Add($"Legibility requires relation '{requiredRelation}' to participate in derivation.");
                legibilitySatisfied = false;
            }
        }

        if (artifact.LegibilityProfile.ProjectionIntegrityRequired)
        {
            foreach (var requiredUniverse in artifact.LegibilityProfile.RequiredUniverses)
            {
                if (!orderedRules.Any(rule => string.Equals(rule.SourceUniverseId, requiredUniverse, StringComparison.Ordinal)))
                {
                    issues.Add($"Projection integrity requires a projection rule for universe '{requiredUniverse}'.");
                    legibilitySatisfied = false;
                    projectionIntegritySatisfied = false;
                }
            }
        }

        return legibilitySatisfied;
    }
}
