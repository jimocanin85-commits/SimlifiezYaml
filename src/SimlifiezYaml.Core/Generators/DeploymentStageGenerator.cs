using System.Text;
using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;

namespace SimlifiezYaml.Core.Generators;

public sealed class DeploymentStageGenerator : IStageGenerator
{
    private readonly IArtifactYamlService _artifactService;
    private readonly IHealthCheckYamlService _healthCheckService;
    private readonly IRollbackYamlService _rollbackService;
    private readonly IDeploymentStrategyService _strategyService;
    private readonly IVariableGroupService _variableGroupService;

    public DeploymentStageGenerator(
        IArtifactYamlService artifactService,
        IHealthCheckYamlService healthCheckService,
        IRollbackYamlService rollbackService,
        IDeploymentStrategyService strategyService,
        IVariableGroupService variableGroupService)
    {
        _artifactService = artifactService;
        _healthCheckService = healthCheckService;
        _rollbackService = rollbackService;
        _strategyService = strategyService;
        _variableGroupService = variableGroupService;
    }

    public string StageName => "Deploy";

    public string Generate(PipelineDefinition definition)
    {
        var sb = new StringBuilder();
        var previousStage = "Artifact";
        foreach (var env in definition.Environments)
        {
            var envId = YamlBuilder.ToIdentifier(env);
            var stageName = $"Deploy_{envId}";
            var steps = new StringBuilder();
            steps.AppendLine(string.Join(Environment.NewLine, _artifactService.GenerateDownloadSteps(definition.Artifact, env)));

            if (definition.Rollback.Enabled)
                steps.AppendLine(string.Join(Environment.NewLine, _rollbackService.GenerateBackupSteps(definition.Rollback, env)));

            var deployTask = YamlBuilder.PowerShellStep(
                $"Write-Host 'Deploying to {env} environment'",
                $"Deploy to {env}");
            steps.AppendLine(string.Join(Environment.NewLine,
                _strategyService.GenerateStrategySteps(definition.DeploymentStrategy, env, deployTask)));

            foreach (var hc in definition.HealthChecks.Where(h => h.Enabled))
                steps.AppendLine(string.Join(Environment.NewLine, _healthCheckService.GenerateHealthCheckSteps(hc)));

            var envGroups = definition.VariableGroups
                .Where(g => g.Scope == Enums.VariableGroupScope.Environment && g.EnvironmentName == env)
                .ToList();
            var stageVars = envGroups.Count > 0
                ? $"  variables:\n{_variableGroupService.GenerateStageVariables(envGroups)}"
                : string.Empty;

            var dependsOn = previousStage;
            var condition = env == "prod" ? "and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))" : "succeeded()";

            sb.AppendLine($"""
- stage: {stageName}
  displayName: 'Deploy {env}'
  dependsOn: {dependsOn}
  condition: {condition}
{stageVars}
  jobs:
  - deployment: DeployTo{envId}
    displayName: 'Deploy to {env}'
    environment: {env}
    strategy:
      runOnce:
        deploy:
          steps:
{YamlBuilder.Indent(steps.ToString(), 12)}
""");
            previousStage = stageName;
        }
        return sb.ToString().TrimEnd();
    }
}
