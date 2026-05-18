using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Abstractions;

public interface IVariableGroupService
{
    string GeneratePipelineVariables(PipelineDefinition definition);
    string GenerateStageVariables(IReadOnlyList<VariableGroupConfig> groups);
    IReadOnlyList<ValidationResult> Validate(IReadOnlyList<VariableGroupConfig> groups, GovernancePolicyConfig? governance);
}

public interface IKeyVaultYamlService
{
    string GeneratePreJobSteps(KeyVaultConfig config);
    IReadOnlyList<ValidationResult> Validate(KeyVaultConfig? config);
}

public interface IArtifactYamlService
{
    IReadOnlyList<string> GeneratePublishSteps(ArtifactConfig config);
    IReadOnlyList<string> GenerateDownloadSteps(ArtifactConfig config, string? environment = null);
}

public interface IRollbackYamlService
{
    IReadOnlyList<string> GenerateBackupSteps(RollbackConfig config, string environment);
    IReadOnlyList<string> GenerateRollbackSteps(RollbackConfig config);
}

public interface IHealthCheckYamlService
{
    IReadOnlyList<string> GenerateHealthCheckSteps(HealthCheckConfig config);
}

public interface INotificationYamlService
{
    IReadOnlyList<string> GenerateNotificationSteps(NotificationConfig config, bool succeeded);
}

public interface IIacYamlService
{
    IReadOnlyList<string> GenerateIacSteps(InfrastructureAsCodeConfig config, string environment);
}

public interface IDeploymentStrategyService
{
    IReadOnlyList<string> GenerateStrategySteps(DeploymentStrategyConfig config, string environment, string deploymentTaskYaml);
    string GetStrategyNote(DeploymentStrategyConfig config);
}

public interface IGovernanceValidationService
{
    IReadOnlyList<ValidationResult> Validate(PipelineDefinition definition, string yaml);
}

public interface IRepoScannerService
{
    RepoScanResult ScanDirectory(string rootPath);
    RepoScanResult ScanFileList(IReadOnlyList<string> relativePaths);
}

public interface IYamlExplanationService
{
    IReadOnlyList<YamlBlockExplanation> ExplainYaml(string yaml);
    string ExplainTask(string taskName);
}

public interface IAgentDiagnosticsService
{
    string GenerateDiagnosticScript(AgentDiagnosticConfig config);
}

public interface ISecretsGovernanceService
{
    IReadOnlyList<SecretGovernanceResult> ScanYaml(string yaml);
    IReadOnlyList<ValidationResult> ToValidationResults(IReadOnlyList<SecretGovernanceResult> results);
}

public interface IEnvironmentYamlService
{
    string GenerateEnvironmentReference(string environmentName);
    string GetApprovalUiNote();
}

public interface ITemplateMarketplaceService
{
    IReadOnlyList<PipelineTemplate> GetAllTemplates();
    IReadOnlyList<PipelineTemplate> GetByCategory(TemplateCategory category);
    PipelineTemplate? GetById(string id);
}

public interface IPipelineGeneratorService
{
    GeneratedPipeline Generate(PipelineDefinition definition);
}
