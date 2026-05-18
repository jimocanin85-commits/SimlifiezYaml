using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Web.State;

public sealed class WizardState
{
    public WizardStep CurrentStep { get; set; } = WizardStep.ProjectType;
    public PipelineDefinition Definition { get; set; } = CreateDefault();
    public GeneratedPipeline? Result { get; set; }
    public PipelineDependencyGraph? DependencyGraph { get; set; }
    public RepoScanResult? ScanResult { get; set; }
    public string RepoPathsInput { get; set; } = "src/MyApp.csproj\nDockerfile\ntests/MyApp.Tests.csproj";

    public static PipelineDefinition CreateDefault() => new()
    {
        Name = "enterprise-pipeline",
        ProjectType = ProjectType.DotNet,
        BuildAgent = BuildAgentType.MicrosoftHosted,
        DeploymentTarget = DeploymentTarget.Hybrid,
        Environments = new[] { "test", "preprod", "prod" },
        DotNetProjectPath = "**/*.csproj",
        SolutionPath = "**/*Tests*.csproj",
        VariableGroups = new[]
        {
            new VariableGroupConfig { Name = "vg-test", Scope = VariableGroupScope.Pipeline },
            new VariableGroupConfig { Name = "vg-prod-secrets", Scope = VariableGroupScope.Environment, EnvironmentName = "prod", ContainsSecrets = true }
        },
        Artifact = new ArtifactConfig { ArtifactType = ArtifactType.PipelineArtifact, ArtifactName = "drop" },
        Rollback = new RollbackConfig { Enabled = true, BackupPath = @"D:\backups", Target = RollbackTarget.Iis, RetentionCount = 5 },
        HealthChecks = new[]
        {
            new HealthCheckConfig { Enabled = true, HealthCheckType = HealthCheckType.HttpEndpoint, Url = "https://myapp-test.contoso.com/health", ExpectedStatusCode = 200 }
        },
        Notifications = new[]
        {
            new NotificationConfig { NotificationType = NotificationType.TeamsWebhook, TeamsWebhookVariable = "TEAMS_WEBHOOK_URL", NotifyOnFailure = true }
        },
        Governance = new GovernancePolicyConfig
        {
            RequiredApprovals = true,
            RequireHealthCheck = true,
            RequireRollback = true,
            RequiredVariableGroups = new[] { "vg-test" },
            ForbiddenTasks = new[] { "CmdLine@2" }
        },
        AgentDiagnostics = new AgentDiagnosticConfig
        {
            CheckWinRm = true,
            CheckIisModule = true,
            DeploymentFolders = new[] { @"D:\deploy", @"C:\inetpub\wwwroot" }
        }
    };
}
