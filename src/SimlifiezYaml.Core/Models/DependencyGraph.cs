namespace SimlifiezYaml.Core.Models;

public sealed class DependencyGraphNode
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NodeType { get; set; } = "stage";
    public IReadOnlyList<string> DependsOn { get; set; } = Array.Empty<string>();
    public string? Condition { get; set; }
    public bool IsParallel { get; set; }
    public bool ManualPromotion { get; set; }
}

public sealed class PipelineDependencyGraph
{
    public IReadOnlyList<DependencyGraphNode> Nodes { get; set; } = Array.Empty<DependencyGraphNode>();

    public static PipelineDependencyGraph FromDefinition(PipelineDefinition definition)
    {
        var nodes = new List<DependencyGraphNode>
        {
            new() { Id = "Build", DisplayName = "Build", NodeType = "stage" },
            new() { Id = "Test", DisplayName = "Test", DependsOn = new[] { "Build" }, Condition = "succeeded()" },
            new() { Id = "Artifact", DisplayName = "Artifact", DependsOn = new[] { "Test" }, Condition = "succeeded()" }
        };

        string? previous = "Artifact";
        foreach (var env in definition.Environments)
        {
            var id = $"Deploy_{Yaml.YamlBuilder.ToIdentifier(env)}";
            nodes.Add(new DependencyGraphNode
            {
                Id = id,
                DisplayName = $"Deploy {env}",
                DependsOn = previous != null ? new[] { previous } : Array.Empty<string>(),
                Condition = env is "prod" ? "succeeded() + main branch" : "succeeded()",
                ManualPromotion = env is "preprod" or "prod"
            });
            previous = id;
        }

        foreach (var custom in definition.StageDependencies)
        {
            var existing = nodes.FirstOrDefault(n => n.Id == custom.StageName);
            if (existing is DependencyGraphNode node)
            {
                nodes[nodes.IndexOf(node)] = new DependencyGraphNode
                {
                    Id = node.Id,
                    DisplayName = node.DisplayName,
                    DependsOn = custom.DependsOn.Count > 0 ? custom.DependsOn : node.DependsOn,
                    Condition = custom.Condition ?? node.Condition,
                    IsParallel = custom.IsParallel,
                    ManualPromotion = custom.ManualPromotion
                };
            }
        }

        return new PipelineDependencyGraph { Nodes = nodes };
    }
}
