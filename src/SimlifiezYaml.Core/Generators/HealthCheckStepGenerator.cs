using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Generators;

public sealed class HealthCheckStepGenerator : IStepGenerator
{
    private readonly IHealthCheckYamlService _healthCheckService;

    public HealthCheckStepGenerator(IHealthCheckYamlService healthCheckService) => _healthCheckService = healthCheckService;

    public IReadOnlyList<string> GenerateSteps(PipelineDefinition definition, string? environment = null)
        => definition.HealthChecks
            .Where(h => h.Enabled)
            .SelectMany(h => _healthCheckService.GenerateHealthCheckSteps(h))
            .ToList();
}
