namespace Hdt.Core.Validation;

public sealed record ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationIssue> Errors { get; init; } = [];
}
