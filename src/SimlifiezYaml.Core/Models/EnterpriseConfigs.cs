using SimlifiezYaml.Core.Enums;

namespace SimlifiezYaml.Core.Models;

public sealed class VariableGroupConfig
{
    public string Name { get; set; } = string.Empty;
    public VariableGroupScope Scope { get; set; } = VariableGroupScope.Pipeline;
    public string? EnvironmentName { get; set; }
    public bool ContainsSecrets { get; set; }
}

public sealed class KeyVaultConfig
{
    public string ServiceConnection { get; set; } = "$(AZURE_SERVICE_CONNECTION)";
    public string KeyVaultName { get; set; } = string.Empty;
    public string SecretsFilter { get; set; } = "*";
    public bool RunAsPreJob { get; set; } = true;
}

public sealed class ArtifactConfig
{
    public ArtifactType ArtifactType { get; set; } = ArtifactType.PipelineArtifact;
    public string ArtifactName { get; set; } = "drop";
    public string? PackagePath { get; set; }
    public string? PublishPath { get; set; }
    public string? DownloadPath { get; set; }
}

public sealed class RollbackConfig
{
    public bool Enabled { get; set; }
    public string? BackupPath { get; set; }
    public string? RollbackScript { get; set; }
    public bool RestorePreviousArtifact { get; set; }
    public int RetentionCount { get; set; } = 3;
    public RollbackTarget Target { get; set; } = RollbackTarget.Iis;
}

public sealed class HealthCheckConfig
{
    public bool Enabled { get; set; }
    public HealthCheckType HealthCheckType { get; set; } = HealthCheckType.HttpEndpoint;
    public string? Url { get; set; }
    public int ExpectedStatusCode { get; set; } = 200;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public string? ServiceName { get; set; }
    public string? AppPoolName { get; set; }
    public int? Port { get; set; }
    public string? CustomScript { get; set; }
}

public sealed class NotificationConfig
{
    public NotificationType NotificationType { get; set; }
    public string? WebhookUrlVariable { get; set; }
    public string? TeamsWebhookVariable { get; set; }
    public IReadOnlyList<string> EmailRecipients { get; set; } = Array.Empty<string>();
    public bool NotifyOnSuccess { get; set; }
    public bool NotifyOnFailure { get; set; } = true;
}

public sealed class InfrastructureAsCodeConfig
{
    public IaCTool Tool { get; set; } = IaCTool.Terraform;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool PlanOnly { get; set; }
    public bool ApplyOnApproval { get; set; } = true;
    public string ServiceConnection { get; set; } = "$(AZURE_SERVICE_CONNECTION)";
    public string? BackendConfig { get; set; }
}

public sealed class DeploymentStrategyConfig
{
    public DeploymentStrategyType StrategyType { get; set; } = DeploymentStrategyType.Standard;
    public int BatchSize { get; set; } = 1;
    public int CanaryPercentage { get; set; } = 10;
    public string? SlotName { get; set; } = "staging";
    public bool RollbackOnFailure { get; set; } = true;
}

public sealed class GovernancePolicyConfig
{
    public bool RequiredApprovals { get; set; } = true;
    public IReadOnlyList<string> RequiredVariableGroups { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ForbiddenTasks { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredTasks { get; set; } = Array.Empty<string>();
    public string? NamingConvention { get; set; }
    public bool RequireHealthCheck { get; set; } = true;
    public bool RequireRollback { get; set; }
}

public sealed class RepoScanResult
{
    public ProjectType ProjectType { get; set; }
    public string? FrameworkVersion { get; set; }
    public bool HasDockerfile { get; set; }
    public bool HasTests { get; set; }
    public bool HasTerraform { get; set; }
    public bool HasBicep { get; set; }
    public IReadOnlyList<string> SuggestedTemplates { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedFiles { get; set; } = Array.Empty<string>();
}

public sealed class AgentDiagnosticConfig
{
    public bool CheckPowerShellVersion { get; set; } = true;
    public bool CheckWinRm { get; set; } = true;
    public bool CheckDocker { get; set; }
    public bool CheckIisModule { get; set; } = true;
    public bool CheckNetworkAccess { get; set; } = true;
    public bool CheckPermissions { get; set; } = true;
    public IReadOnlyList<string> DeploymentFolders { get; set; } = Array.Empty<string>();
}

public sealed class SecretGovernanceResult
{
    public bool HasPlainTextSecret { get; set; }
    public string? SecretLocation { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public SecretSeverity Severity { get; set; }
}
