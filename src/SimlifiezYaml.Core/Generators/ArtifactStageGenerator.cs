using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Generators;

public sealed class ArtifactStageGenerator : IStageGenerator
{
    private readonly IArtifactYamlService _artifactService;

    public ArtifactStageGenerator(IArtifactYamlService artifactService) => _artifactService = artifactService;

    public string StageName => "Artifact";

    public string Generate(PipelineDefinition definition)
    {
        var steps = string.Join(Environment.NewLine, _artifactService.GeneratePublishSteps(definition.Artifact));
        return $"""
- stage: Artifact
  displayName: 'Publish artifacts'
  dependsOn: Test
  condition: succeeded()
  jobs:
  - job: PublishArtifact
    displayName: 'Publish build output'
    steps:
{Yaml.YamlBuilder.Indent(steps, 6)}
""";
    }
}
