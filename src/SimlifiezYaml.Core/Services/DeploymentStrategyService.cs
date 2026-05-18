using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;

namespace SimlifiezYaml.Core.Services;

public sealed class DeploymentStrategyService : IDeploymentStrategyService
{
    public IReadOnlyList<string> GenerateStrategySteps(DeploymentStrategyConfig config, string environment, string deploymentTaskYaml)
    {
        return config.StrategyType switch
        {
            DeploymentStrategyType.Standard => new[] { deploymentTaskYaml },
            DeploymentStrategyType.Rolling => new[]
            {
                YamlBuilder.PowerShellStep($"""
$batchSize = {config.BatchSize}
Write-Host "Rolling deployment to {environment} with batch size $batchSize"
""", "Rolling deployment - batch orchestration"),
                deploymentTaskYaml
            },
            DeploymentStrategyType.BlueGreen => new[]
            {
                YamlBuilder.PowerShellStep($"Write-Host 'Blue-green: deploy to inactive environment for {environment}'", "Blue-green deploy"),
                deploymentTaskYaml,
                YamlBuilder.PowerShellStep("Write-Host 'Blue-green: switch traffic after health check'", "Blue-green traffic switch")
            },
            DeploymentStrategyType.Canary => new[]
            {
                YamlBuilder.PowerShellStep($"Write-Host 'Canary: route {config.CanaryPercentage}% traffic for {environment}'", "Canary deployment"),
                deploymentTaskYaml
            },
            DeploymentStrategyType.SlotSwap => new[]
            {
                deploymentTaskYaml,
                YamlBuilder.Task("AzureAppServiceManage@0", new Dictionary<string, string>
                {
                    ["Action"] = "Swap Slots",
                    ["WebAppName"] = "$(WEBAPP_NAME)",
                    ["ResourceGroupName"] = "$(RESOURCE_GROUP)",
                    ["SourceSlot"] = config.SlotName ?? "staging",
                    ["SwapWithProduction"] = "true"
                }, "Swap deployment slots")
            },
            _ => new[] { deploymentTaskYaml }
        };
    }

    public string GetStrategyNote(DeploymentStrategyConfig config) =>
        config.StrategyType switch
        {
            DeploymentStrategyType.Canary => $"Canary releases {config.CanaryPercentage}% traffic before full promotion.",
            DeploymentStrategyType.SlotSwap => "Slot swap requires staging slot deployment and health validation.",
            DeploymentStrategyType.BlueGreen => "Blue-green keeps two environments; switch only after health checks pass.",
            _ => $"Using {config.StrategyType} deployment strategy."
        };
}
