namespace Hdt.Core.Validation;

public sealed record ValidationIssue(ValidationErrorCode Code, string Message, string? Path = null);
