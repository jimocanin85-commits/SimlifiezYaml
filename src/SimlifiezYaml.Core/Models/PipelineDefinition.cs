using SimlifiezYaml.Core.Enums;

namespace SimlifiezYaml.Core.Models;

public sealed class PipelineEnvironment
{
    public string Name { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public IReadOnlyList<VariableGroupConfig> VariableGroups { get; set; } = Array.Empty<VariableGroupConfig>();
}

public sealed class StageDependency
{
    public string StageName { get; set; } = string.Empty;
    public IReadOnlyList<string> DependsOn { get; set; } = Array.Empty<string>();
    public string? Condition { get; set; }
    public bool IsParallel { get; set; }
    public bool ManualPromotion { get; set; }
}

public sealed class PipelineTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TemplateCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<DeploymentTarget> SupportedTargets { get; set; } = Array.Empty<DeploymentTarget>();
    public IReadOnlyList<string> RequiredInputs { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> GeneratedStages { get; set; } = Array.Empty<string>();
    public TemplateRiskLevel RiskLevel { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
}

public sealed class YamlBlockExplanation
{
    public string YamlSnippet { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? TaskName { get; set; }
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
}

public sealed class PipelineDefinition
{
    public string Name { get; set; } = "enterprise-pipeline";
    public ProjectType ProjectType { get; set; }
    public BuildAgentType BuildAgent { get; set; } = BuildAgentType.MicrosoftHosted;
    public string? PoolName { get; set; }
    public DeploymentTarget DeploymentTarget { get; set; }
    public TriggerConfig Trigger { get; set; } = TriggerConfig.Default;
    public IReadOnlyList<string> Environments { get; set; } = new[] { "test", "preprod", "prod" };
    public IReadOnlyList<VariableGroupConfig> VariableGroups { get; set; } = Array.Empty<VariableGroupConfig>();
    public KeyVaultConfig? KeyVault { get; set; }
    public ArtifactConfig Artifact { get; set; } = new();
    public RollbackConfig Rollback { get; set; } = new();
    public IReadOnlyList<HealthCheckConfig> HealthChecks { get; set; } = Array.Empty<HealthCheckConfig>();
    public IReadOnlyList<NotificationConfig> Notifications { get; set; } = Array.Empty<NotificationConfig>();
    public InfrastructureAsCodeConfig? IaC { get; set; }
    public DeploymentStrategyConfig DeploymentStrategy { get; set; } = new();
    public GovernancePolicyConfig Governance { get; set; } = new();
    public AgentDiagnosticConfig? AgentDiagnostics { get; set; }
    public IReadOnlyList<StageDependency> StageDependencies { get; set; } = Array.Empty<StageDependency>();
    public string? DotNetProjectPath { get; set; }
    public string? SolutionPath { get; set; }
}

public sealed class GeneratedPipeline
{
    public string Yaml { get; set; } = string.Empty;
    public IReadOnlyList<YamlBlockExplanation> Explanations { get; set; } = Array.Empty<YamlBlockExplanation>();
    public IReadOnlyList<ValidationResult> ValidationResults { get; set; } = Array.Empty<ValidationResult>();
    public string? DiagnosticScript { get; set; }
}
