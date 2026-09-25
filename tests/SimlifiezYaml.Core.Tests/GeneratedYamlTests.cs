using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.DependencyInjection;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using Xunit;
using YamlDotNet.Serialization;

namespace SimlifiezYaml.Core.Tests;

/// <summary>
/// End-to-end checks on the generated pipeline: the YAML must parse, stage names and
/// dependencies must be valid, and scripts must reach the agent unmangled.
/// </summary>
public class GeneratedYamlTests
{
    private static readonly Regex StageIdentifier = new("^[A-Za-z0-9_]+$");
    private readonly IPipelineGeneratorService _generator;

    public GeneratedYamlTests()
    {
        var services = new ServiceCollection();
        services.AddSimlifiezYamlCore();
        _generator = services.BuildServiceProvider().GetRequiredService<IPipelineGeneratorService>();
    }

    public static IEnumerable<object[]> AllOptionCombinations()
    {
        foreach (var strategy in Enum.GetValues<DeploymentStrategyType>())
        foreach (var artifact in Enum.GetValues<ArtifactType>())
        foreach (var rollback in Enum.GetValues<RollbackTarget>())
            yield return new object[] { strategy, artifact, rollback };
    }

    [Theory]
    [MemberData(nameof(AllOptionCombinations))]
    public void GeneratedYaml_IsValidForEveryOptionCombination(
        DeploymentStrategyType strategy, ArtifactType artifact, RollbackTarget rollback)
    {
        var definition = FullDefinition();
        definition.DeploymentStrategy = new DeploymentStrategyConfig { StrategyType = strategy };
        definition.Artifact = new ArtifactConfig { ArtifactType = artifact, ArtifactName = "drop" };
        definition.Rollback = new RollbackConfig { Enabled = true, Target = rollback, BackupPath = @"D:\backups" };

        var yaml = _generator.Generate(definition).Yaml;

        var root = Parse(yaml);
        Assert.True(root.ContainsKey("trigger"), "Missing trigger");
        var stages = StagesOf(root);
        Assert.NotEmpty(stages);

        var names = stages.Select(s => (string)s["stage"]).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.All(names, n => Assert.Matches(StageIdentifier, n));

        foreach (var stage in stages)
        {
            foreach (var dependency in DependenciesOf(stage))
                Assert.Contains(dependency, names);
        }

        foreach (var script in PowerShellScripts(root))
        {
            Assert.DoesNotContain("$([", script);          // Azure macros must not be mangled
            Assert.DoesNotContain("''", script);             // no doubled quotes from over-escaping
            Assert.DoesNotContain(@"\\inetpub", script);     // no doubled backslashes in local paths
        }
    }

    [Fact]
    public void GeneratedYaml_HealthCheckScriptKeepsUrlIntact()
    {
        var yaml = _generator.Generate(FullDefinition()).Yaml;
        var scripts = PowerShellScripts(Parse(yaml)).ToList();

        Assert.Contains(scripts, s => s.Contains("$uri = 'https://myapp.contoso.com/health'"));
        Assert.Contains(scripts, s => s.Contains("#$(Build.BuildNumber)"));
    }

    [Fact]
    public void GeneratedYaml_HyphenatedEnvironmentGetsValidStageName()
    {
        var yaml = _generator.Generate(FullDefinition()).Yaml;
        var stages = StagesOf(Parse(yaml));

        var preProd = Assert.Single(stages, s => (string)s["stage"] == "Deploy_pre_prod");
        var job = Assert.Single(JobsOf(preProd));
        Assert.Equal("pre-prod", job["environment"]);
        Assert.Equal("DeployTopre_prod", job["deployment"]);
    }

    [Fact]
    public void GeneratedYaml_NotificationsAreSplitBySuccessAndFailure()
    {
        var yaml = _generator.Generate(FullDefinition()).Yaml;
        var stages = StagesOf(Parse(yaml));

        var success = Assert.Single(stages, s => (string)s["stage"] == "Notify_Success");
        Assert.Equal("succeeded()", success["condition"]);
        Assert.Contains("succeeded", string.Join("\n", PowerShellScripts(success)));
        Assert.DoesNotContain(" failed", string.Join("\n", PowerShellScripts(success)));

        var failure = Assert.Single(stages, s => (string)s["stage"] == "Notify_Failure");
        Assert.Equal("failed()", failure["condition"]);
        var failureDependencies = DependenciesOf(failure).ToList();
        Assert.Contains("Build", failureDependencies);
        Assert.Contains("Deploy_prod", failureDependencies);
        Assert.DoesNotContain("# condition:", yaml);
    }

    private static PipelineDefinition FullDefinition() => new()
    {
        Name = "enterprise-pipeline",
        ProjectType = ProjectType.DotNet,
        BuildAgent = BuildAgentType.MicrosoftHosted,
        DeploymentTarget = DeploymentTarget.Hybrid,
        Environments = new[] { "test", "pre-prod", "prod" },
        VariableGroups = new[]
        {
            new VariableGroupConfig { Name = "vg-common", Scope = VariableGroupScope.Pipeline },
            new VariableGroupConfig { Name = "vg-prod", Scope = VariableGroupScope.Environment, EnvironmentName = "prod" }
        },
        KeyVault = new KeyVaultConfig { KeyVaultName = "kv-test" },
        IaC = new InfrastructureAsCodeConfig { Tool = IaCTool.Terraform, WorkingDirectory = "infra" },
        Rollback = new RollbackConfig { Enabled = true, BackupPath = @"D:\backups", Target = RollbackTarget.Iis },
        HealthChecks = new[]
        {
            new HealthCheckConfig { Enabled = true, HealthCheckType = HealthCheckType.HttpEndpoint, Url = "https://myapp.contoso.com/health" },
            new HealthCheckConfig { Enabled = true, HealthCheckType = HealthCheckType.IisAppPool, AppPoolName = "MyApp" },
            new HealthCheckConfig { Enabled = true, HealthCheckType = HealthCheckType.WindowsService, ServiceName = "MyService" },
            new HealthCheckConfig { Enabled = true, HealthCheckType = HealthCheckType.PortCheck, Port = 443 }
        },
        Notifications = new[]
        {
            new NotificationConfig { NotificationType = NotificationType.TeamsWebhook, NotifyOnSuccess = true, NotifyOnFailure = true },
            new NotificationConfig { NotificationType = NotificationType.CustomWebhook, NotifyOnFailure = true }
        }
    };

    private static Dictionary<object, object> Parse(string yaml)
    {
        var parsed = new DeserializerBuilder().Build().Deserialize<object>(yaml);
        return Assert.IsType<Dictionary<object, object>>(parsed);
    }

    private static List<Dictionary<object, object>> StagesOf(Dictionary<object, object> root) =>
        Assert.IsType<List<object>>(root["stages"]).Cast<Dictionary<object, object>>().ToList();

    private static List<Dictionary<object, object>> JobsOf(Dictionary<object, object> stage) =>
        Assert.IsType<List<object>>(stage["jobs"]).Cast<Dictionary<object, object>>().ToList();

    private static IEnumerable<string> DependenciesOf(Dictionary<object, object> stage) =>
        stage.TryGetValue("dependsOn", out var value) switch
        {
            false => Array.Empty<string>(),
            true when value is string single => new[] { single },
            true when value is List<object> list => list.Cast<string>(),
            _ => Array.Empty<string>()
        };

    /// <summary>Finds every <c>powershell:</c> script anywhere in the document.</summary>
    private static IEnumerable<string> PowerShellScripts(object node)
    {
        switch (node)
        {
            case Dictionary<object, object> map:
                foreach (var (key, value) in map)
                {
                    if (key is "powershell" && value is string script)
                        yield return script;
                    else
                        foreach (var nested in PowerShellScripts(value))
                            yield return nested;
                }
                break;
            case List<object> list:
                foreach (var item in list)
                foreach (var nested in PowerShellScripts(item))
                    yield return nested;
                break;
        }
    }
}
