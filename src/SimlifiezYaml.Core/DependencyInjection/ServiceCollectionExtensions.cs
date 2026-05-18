using Microsoft.Extensions.DependencyInjection;
using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Generators;
using SimlifiezYaml.Core.Services;

namespace SimlifiezYaml.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimlifiezYamlCore(this IServiceCollection services)
    {
        services.AddSingleton<IVariableGroupService, VariableGroupService>();
        services.AddSingleton<IKeyVaultYamlService, KeyVaultYamlService>();
        services.AddSingleton<IArtifactYamlService, ArtifactYamlService>();
        services.AddSingleton<IRollbackYamlService, RollbackYamlService>();
        services.AddSingleton<IHealthCheckYamlService, HealthCheckYamlService>();
        services.AddSingleton<INotificationYamlService, NotificationYamlService>();
        services.AddSingleton<IIacYamlService, IacYamlService>();
        services.AddSingleton<IDeploymentStrategyService, DeploymentStrategyService>();
        services.AddSingleton<IGovernanceValidationService, GovernanceValidationService>();
        services.AddSingleton<IRepoScannerService, RepoScannerService>();
        services.AddSingleton<IYamlExplanationService, YamlExplanationService>();
        services.AddSingleton<IAgentDiagnosticsService, AgentDiagnosticsService>();
        services.AddSingleton<ISecretsGovernanceService, SecretsGovernanceService>();
        services.AddSingleton<IEnvironmentYamlService, EnvironmentYamlService>();
        services.AddSingleton<ITemplateMarketplaceService, TemplateMarketplaceService>();
        services.AddSingleton<IPipelineGeneratorService, PipelineGeneratorService>();

        services.AddSingleton<BuildStageGenerator>();
        services.AddSingleton<TestStageGenerator>();
        services.AddSingleton<ArtifactStageGenerator>();
        services.AddSingleton<DeploymentStageGenerator>();
        services.AddSingleton<RollbackStepGenerator>();
        services.AddSingleton<HealthCheckStepGenerator>();
        services.AddSingleton<NotificationStepGenerator>();
        services.AddSingleton<GovernanceValidator>();

        return services;
    }
}
