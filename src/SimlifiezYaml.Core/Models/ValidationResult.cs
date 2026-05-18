using SimlifiezYaml.Core.Enums;

namespace SimlifiezYaml.Core.Models;

public sealed class ValidationResult
{
    public ValidationSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? AffectedField { get; init; }
    public string? SuggestedFix { get; init; }
}
