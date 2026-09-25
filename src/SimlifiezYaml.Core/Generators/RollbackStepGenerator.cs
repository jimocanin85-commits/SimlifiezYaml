using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Generators;

public sealed class RollbackStepGenerator : IStepGenerator
{
    private readonly IRollbackYamlService _rollbackService;

    public RollbackStepGenerator(IRollbackYamlService rollbackService) => _rollbackService = rollbackService;

    public IReadOnlyList<string> GenerateSteps(PipelineDefinition definition, string? environment = null)
    {
        if (!definition.Rollback.Enabled)
            return Array.Empty<string>();
        var env = environment ?? definition.Environments.LastOrDefault() ?? "prod";
        return _rollbackService.GenerateRollbackSteps(definition.Rollback, definition.Deployment, env);
    }
}
