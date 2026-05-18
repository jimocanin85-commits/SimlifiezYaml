using System.Text.RegularExpressions;
using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Services;

public sealed class GovernanceValidationService : IGovernanceValidationService
{
    public IReadOnlyList<ValidationResult> Validate(PipelineDefinition definition, string yaml)
    {
        var results = new List<ValidationResult>();
        var governance = definition.Governance;

        if (governance.RequiredApprovals)
        {
            foreach (var env in new[] { "preprod", "prod" })
            {
                if (definition.Environments.Contains(env, StringComparer.OrdinalIgnoreCase)
                    && !yaml.Contains($"environment: {env}", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new ValidationResult
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"Environment '{env}' should use Azure DevOps environment approvals.",
                        AffectedField = "Environments",
                        SuggestedFix = "Configure approvals in Azure DevOps Environments for preprod and prod."
                    });
                }
            }
        }

        if (governance.RequireHealthCheck && definition.Environments.Contains("prod"))
        {
            if (!definition.HealthChecks.Any(h => h.Enabled))
            {
                results.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Production deployments require at least one enabled health check.",
                    AffectedField = nameof(PipelineDefinition.HealthChecks),
                    SuggestedFix = "Enable an HTTP, IIS, or service health check before prod deployment."
                });
            }
        }

        if (governance.RequireRollback && definition.DeploymentTarget == DeploymentTarget.OnPrem)
        {
            if (!definition.Rollback.Enabled)
            {
                results.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = "On-premises deployments require rollback configuration.",
                    AffectedField = nameof(PipelineDefinition.Rollback),
                    SuggestedFix = "Enable rollback and specify a backup path or rollback script."
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(governance.NamingConvention))
        {
            try
            {
                if (!Regex.IsMatch(definition.Name, governance.NamingConvention))
                {
                    results.Add(new ValidationResult
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = $"Pipeline name '{definition.Name}' does not match naming convention.",
                        AffectedField = nameof(PipelineDefinition.Name),
                        SuggestedFix = $"Rename pipeline to match pattern: {governance.NamingConvention}"
                    });
                }
            }
            catch (RegexParseException)
            {
                results.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Governance naming convention regex is invalid.",
                    AffectedField = nameof(GovernancePolicyConfig.NamingConvention),
                    SuggestedFix = "Provide a valid regular expression for pipeline naming."
                });
            }
        }

        foreach (var forbidden in governance.ForbiddenTasks)
        {
            if (yaml.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = $"Forbidden task '{forbidden}' detected in generated YAML.",
                    AffectedField = "Yaml",
                    SuggestedFix = $"Remove or replace task '{forbidden}' per governance policy."
                });
            }
        }

        foreach (var required in governance.RequiredTasks)
        {
            if (!yaml.Contains(required, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Warning,
                    Message = $"Required task '{required}' is missing from generated YAML.",
                    AffectedField = "Yaml",
                    SuggestedFix = $"Add task '{required}' to the appropriate stage."
                });
            }
        }

        results.AddRange(new VariableGroupService().Validate(definition.VariableGroups, governance));
        return results;
    }
}
