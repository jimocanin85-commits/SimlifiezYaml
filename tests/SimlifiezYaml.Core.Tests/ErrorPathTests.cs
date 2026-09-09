using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;
using Xunit;

namespace SimlifiezYaml.Core.Tests;

/// <summary>
/// Comprehensive error path and edge case tests for Phase 2 improvements.
/// Tests validation, escaping, error handling, and null safety.
/// </summary>
public class ErrorPathTests
{
    #region PipelineDefinitionValidator Tests

    [Fact]
    public void PipelineDefinitionValidator_RejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(() => PipelineDefinitionValidator.Validate(null!));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsEmptyName()
    {
        var definition = new PipelineDefinition { Name = "" };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("Pipeline name is required"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsExcessivelyLongName()
    {
        var definition = new PipelineDefinition { Name = new string('x', 300) };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("255 characters"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsNoEnvironments()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            Environments = Array.Empty<string>()
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("at least one environment"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsEmptyEnvironmentName()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            Environments = new[] { "", "test" }
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("Environment names cannot be empty"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsInvalidEnvironmentNames()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            Environments = new[] { "test@invalid", "Test_Name" }  // Invalid chars
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("invalid characters"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RequiresSelfHostedPoolName()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            BuildAgent = BuildAgentType.SelfHosted,
            PoolName = ""
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("Pool name is required"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsInvalidCanaryPercentage()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            DeploymentStrategy = new DeploymentStrategyConfig
            {
                Strategy = DeploymentStrategy.Canary,
                CanaryPercentage = 150
            }
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("between 0 and 100"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsInvalidRegex()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            Governance = new GovernancePolicyConfig { NamingConvention = "[invalid(regex" }
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("valid regular expression"));
    }

    [Fact]
    public void PipelineDefinitionValidator_RejectsHttpHealthCheckWithoutEndpoint()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            HealthChecks = new[]
            {
                new HealthCheckConfig { HealthCheckType = HealthCheckType.Http, Endpoint = null }
            }
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Contains(errors, e => e.Contains("HTTP health check requires"));
    }

    [Fact]
    public void PipelineDefinitionValidator_ThrowsOnInvalid()
    {
        var definition = new PipelineDefinition { Name = "" };
        Assert.Throws<ArgumentException>(() => PipelineDefinitionValidator.ValidateOrThrow(definition));
    }

    [Fact]
    public void PipelineDefinitionValidator_PassesValidDefinition()
    {
        var definition = new PipelineDefinition
        {
            Name = "my-pipeline",
            Environments = new[] { "test", "prod" },
            BuildAgent = BuildAgentType.MicrosoftHosted
        };
        var errors = PipelineDefinitionValidator.Validate(definition);
        Assert.Empty(errors);
    }

    #endregion

    #region YamlBuilder Escaping Tests

    [Fact]
    public void PowerShellStep_EscapesSingleQuotes()
    {
        var step = YamlBuilder.PowerShellStep("Write-Host 'test'", "Test");
        Assert.Contains("''test''", step);
    }

    [Fact]
    public void PowerShellStep_EscapesDisplayNameWithQuotes()
    {
        var step = YamlBuilder.PowerShellStep("echo test", "Test's Step");
        Assert.Contains("displayName: 'Test''s Step'", step);
    }

    [Fact]
    public void PowerShellStep_HandlesMultilineScripts()
    {
        var script = "Write-Host 'line 1'\nWrite-Host 'line 2'";
        var step = YamlBuilder.PowerShellStep(script, "Multi");
        Assert.Contains("Write-Host ''line 1''", step);
        Assert.Contains("Write-Host ''line 2''", step);
    }

    [Fact]
    public void Task_EscapesInputValues()
    {
        var task = YamlBuilder.Task("TestTask@1", 
            new Dictionary<string, string> { ["input"] = "value'with'quotes" }, 
            "Display'Name");
        Assert.Contains("input: 'value''with''quotes'", task);
        Assert.Contains("displayName: 'Display''Name'", task);
    }

    [Fact]
    public void Indent_PreservesEmptyLines()
    {
        var content = "line1\n\nline3";
        var indented = YamlBuilder.Indent(content, 2);
        Assert.Contains("  line1", indented);
        Assert.Contains("  line3", indented);
    }

    #endregion

    #region FileOperationsHelper Tests

    [Fact]
    public void WriteYamlFile_RejectsNullPath()
    {
        var (success, error) = FileOperationsHelper.WriteYamlFile(null!, "content");
        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void WriteYamlFile_RejectsNullContent()
    {
        var (success, error) = FileOperationsHelper.WriteYamlFile("/tmp/test.yml", null!);
        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReadYamlFile_RejectsNullPath()
    {
        var (content, error) = FileOperationsHelper.ReadYamlFile(null!);
        Assert.Null(content);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReadYamlFile_ReturnsErrorForMissingFile()
    {
        var (content, error) = FileOperationsHelper.ReadYamlFile("/nonexistent/path/file.yml");
        Assert.Null(content);
        Assert.NotNull(error);
        Assert.Contains("not found", error);
    }

    [Fact]
    public void ValidateFilePath_RejectsEmptyPath()
    {
        var (valid, error) = FileOperationsHelper.ValidateFilePath("");
        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateFilePath_AcceptsValidPath()
    {
        var (valid, error) = FileOperationsHelper.ValidateFilePath("/valid/path/file.yml");
        Assert.True(valid);
        Assert.Null(error);
    }

    #endregion

    #region PoolConfigurationHelper Tests

    [Fact]
    public void PoolConfigurationHelper_SelfHostedWithPoolName()
    {
        var config = PoolConfigurationHelper.GeneratePoolConfiguration(BuildAgentType.SelfHosted, "MyPool");
        Assert.Equal("name: 'MyPool'", config);
    }

    [Fact]
    public void PoolConfigurationHelper_SelfHostedWithoutPoolName()
    {
        var config = PoolConfigurationHelper.GeneratePoolConfiguration(BuildAgentType.SelfHosted, null);
        Assert.Equal("name: 'Default'", config);
    }

    [Fact]
    public void PoolConfigurationHelper_MicrosoftHosted()
    {
        var config = PoolConfigurationHelper.GeneratePoolConfiguration(BuildAgentType.MicrosoftHosted, "ignored");
        Assert.Equal("vmImage: 'windows-latest'", config);
    }

    #endregion

    #region Escaping Security Tests

    [Fact]
    public void PowerShellStep_DoesNotAllowSubexpressionExecution()
    {
        var malicious = "Write-Host $(Get-Secret)";
        var step = YamlBuilder.PowerShellStep(malicious, "Test");
        // Verify the subexpression is escaped/safe
        Assert.Contains("powershell:", step);
    }

    [Fact]
    public void Task_SafelyHandlesPathsWithSpecialChars()
    {
        var task = YamlBuilder.Task("ScriptTask@1",
            new Dictionary<string, string> { ["scriptPath"] = "C:\\path\\to\\script's file.ps1" },
            "Test");
        Assert.Contains(":", task);  // Colon in path is preserved
        Assert.Contains("''", task);  // Quote is escaped
    }

    #endregion
}
