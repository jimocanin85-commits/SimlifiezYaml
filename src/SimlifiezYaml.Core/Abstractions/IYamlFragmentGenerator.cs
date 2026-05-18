using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Abstractions;

public interface IYamlFragmentGenerator
{
    string Generate(PipelineDefinition definition);
}

public interface IStageGenerator : IYamlFragmentGenerator
{
    string StageName { get; }
}

public interface IStepGenerator
{
    IReadOnlyList<string> GenerateSteps(PipelineDefinition definition, string? environment = null);
}
