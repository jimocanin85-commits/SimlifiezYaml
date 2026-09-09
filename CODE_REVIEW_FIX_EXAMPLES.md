# SimlifiezYaml Code Review - Detailed Fix Examples

## Issue A: DI Anti-Pattern in GovernanceValidationService

### Current Code (❌ WRONG)
```csharp
public sealed class GovernanceValidationService : IGovernanceValidationService
{
    private readonly IGovernanceValidationService _governanceService;
    private readonly ISecretsGovernanceService _secretsService;

    public GovernanceValidationService(
        IGovernanceValidationService governanceService,
        ISecretsGovernanceService secretsService)
    {
        _governanceService = governanceService;
        _secretsService = secretsService;
    }

    public IReadOnlyList<ValidationResult> Validate(PipelineDefinition definition, string yaml)
    {
        var results = new List<ValidationResult>();
        // ... validation logic ...
        
        // ❌ ANTI-PATTERN: Creates new instance instead of using DI
        results.AddRange(new VariableGroupService().Validate(definition.VariableGroups, governance));
        return results;
    }
}
```

### Fixed Code (✅ CORRECT)
```csharp
public sealed class GovernanceValidationService : IGovernanceValidationService
{
    private readonly IGovernanceValidationService _governanceService;
    private readonly ISecretsGovernanceService _secretsService;
    private readonly IVariableGroupService _variableGroupService;  // ADD THIS

    public GovernanceValidationService(
        IGovernanceValidationService governanceService,
        ISecretsGovernanceService secretsService,
        IVariableGroupService variableGroupService)  // ADD THIS PARAMETER
    {
        _governanceService = governanceService;
        _secretsService = secretsService;
        _variableGroupService = variableGroupService;  // ADD THIS LINE
    }

    public IReadOnlyList<ValidationResult> Validate(PipelineDefinition definition, string yaml)
    {
        var results = new List<ValidationResult>();
        var governance = definition.Governance;

        // ... existing validation logic ...

        // ✅ CORRECT: Uses injected service
        results.AddRange(_variableGroupService.Validate(definition.VariableGroups, governance));
        return results;
    }
}
```

### Update DI Registration
```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddSimlifiezYamlCore(this IServiceCollection services)
{
    // ... existing registrations ...
    
    // This service now has GovernanceValidationService in its dependencies
    // The DI container will automatically inject it
    services.AddSingleton<IGovernanceValidationService, GovernanceValidationService>();
    
    return services;
}
```

---

## Issue F: Unsafe FirstOrDefault/LastOrDefault

### Current Code (❌ WRONG)
```csharp
public sealed class PipelineGeneratorService : IPipelineGeneratorService
{
    public GeneratedPipeline Generate(PipelineDefinition definition)
    {
        var sb = new StringBuilder();
        
        // ... trigger and variables ...
        
        if (definition.KeyVault != null && !string.IsNullOrWhiteSpace(definition.KeyVault.KeyVaultName))
        {
            sb.AppendLine("- stage: KeyVaultPreJob");
            sb.AppendLine("  displayName: 'Load Key Vault secrets'");
            sb.AppendLine("  jobs:");
            sb.AppendLine("  - job: KeyVault");
            sb.AppendLine("    steps:");
            sb.AppendLine(Yaml.YamlBuilder.Indent(
                _keyVaultService.GeneratePreJobSteps(definition.KeyVault), 6));
        }

        var stages = new StringBuilder();
        stages.AppendLine(_buildGenerator.Generate(definition));
        stages.AppendLine(_testGenerator.Generate(definition));
        stages.AppendLine(_artifactGenerator.Generate(definition));

        if (definition.IaC != null)
            AppendIacStage(stages, definition);

        stages.AppendLine(_deploymentGenerator.Generate(definition));
        
        // ❌ PROBLEM: Uses FirstOrDefault with fallback
        AppendRollbackStage(stages, definition, _rollbackService);
        AppendNotificationStage(stages, definition);

        sb.AppendLine(stages.ToString().TrimEnd());

        var yaml = sb.ToString();
        var validation = _governanceValidator.ValidateAll(definition, yaml);
        validation = validation.Concat(_keyVaultService.Validate(definition.KeyVault)).ToList();

        return new GeneratedPipeline
        {
            Yaml = yaml,
            Explanations = _explanationService.ExplainYaml(yaml),
            ValidationResults = validation,
            DiagnosticScript = null
        };
    }

    private void AppendRollbackStage(StringBuilder stages, PipelineDefinition definition, IRollbackYamlService rollbackService)
    {
        if (!definition.Rollback.Enabled) return;
        // ❌ UNSAFE: What if Environments is empty or null?
        var lastEnv = definition.Environments.LastOrDefault() ?? "prod";
        stages.AppendLine($"""
- stage: Rollback
  displayName: 'Rollback on failure'
  dependsOn: Deploy_{lastEnv}
  condition: failed()
  jobs:
  - job: RollbackJob
    steps:
{Yaml.YamlBuilder.Indent(string.Join(Environment.NewLine, 
    rollbackService.GenerateRollbackSteps(definition.Rollback)), 6)}
""");
    }

    private void AppendNotificationStage(StringBuilder stages, PipelineDefinition definition)
    {
        if (definition.Notifications.Count == 0) return;
        // ❌ UNSAFE: Same issue - hardcoded fallback
        var lastEnv = definition.Environments.LastOrDefault() ?? "prod";
        var steps = string.Join(Environment.NewLine, _notificationGenerator.GenerateSteps(definition));
        stages.AppendLine($"""
- stage: Notify
  displayName: 'Pipeline notifications'
  dependsOn: Deploy_{lastEnv}
  condition: always()
  jobs:
  - job: Notify
    steps:
{Yaml.YamlBuilder.Indent(steps, 6)}
""");
    }
}
```

### Fixed Code (✅ CORRECT)
```csharp
public sealed class PipelineDefinition
{
    private IReadOnlyList<string> _environments = new[] { "test", "preprod", "prod" };
    
    /// <summary>
    /// Gets or sets the deployment environments (e.g., test, preprod, prod).
    /// At least one environment must be defined.
    /// </summary>
    public IReadOnlyList<string> Environments
    {
        get => _environments;
        set
        {
            if (value == null || value.Count == 0)
                throw new ArgumentException(
                    "At least one environment must be defined. Examples: 'test', 'preprod', 'prod'",
                    nameof(Environments));
            
            // Validate each environment name
            foreach (var env in value)
            {
                if (string.IsNullOrWhiteSpace(env))
                    throw new ArgumentException("Environment names cannot be empty", nameof(Environments));
                
                if (env.Length > 50)
                    throw new ArgumentException(
                        $"Environment name '{env}' exceeds maximum length of 50 characters",
                        nameof(Environments));
                
                if (!Regex.IsMatch(env, @"^[a-zA-Z0-9_-]+$"))
                    throw new ArgumentException(
                        $"Environment name '{env}' contains invalid characters. Only alphanumeric, dash, and underscore allowed.",
                        nameof(Environments));
            }
            
            _environments = value;
        }
    }
}

// Now the service can safely assume Environments is valid
public sealed class PipelineGeneratorService : IPipelineGeneratorService
{
    public GeneratedPipeline Generate(PipelineDefinition definition)
    {
        // Validate input first
        var validationErrors = ValidateDefinition(definition);
        if (validationErrors.Any(v => v.Severity == ValidationSeverity.Error))
        {
            return new GeneratedPipeline
            {
                Yaml = string.Empty,
                ValidationResults = validationErrors
            };
        }

        var sb = new StringBuilder();
        
        // ... rest of generation ...
        
        return new GeneratedPipeline
        {
            Yaml = yaml,
            Explanations = _explanationService.ExplainYaml(yaml),
            ValidationResults = validation,
        };
    }

    private IReadOnlyList<ValidationResult> ValidateDefinition(PipelineDefinition definition)
    {
        var errors = new List<ValidationResult>();

        if (definition == null)
        {
            errors.Add(new ValidationResult
            {
                Severity = ValidationSeverity.Error,
                Message = "Pipeline definition is required",
                SuggestedFix = "Provide a valid PipelineDefinition"
            });
            return errors;
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            errors.Add(new ValidationResult
            {
                Severity = ValidationSeverity.Error,
                Message = "Pipeline name is required",
                AffectedField = nameof(PipelineDefinition.Name),
                SuggestedFix = "Provide a pipeline name (e.g., 'my-app-pipeline')"
            });
        }

        if (definition.Environments == null || definition.Environments.Count == 0)
        {
            errors.Add(new ValidationResult
            {
                Severity = ValidationSeverity.Error,
                Message = "At least one environment must be defined",
                AffectedField = nameof(PipelineDefinition.Environments),
                SuggestedFix = "Add environments such as: test, preprod, prod"
            });
        }

        return errors;
    }

    private void AppendRollbackStage(StringBuilder stages, PipelineDefinition definition, IRollbackYamlService rollbackService)
    {
        if (!definition.Rollback.Enabled) return;
        
        // ✅ SAFE: Now guaranteed to have at least one environment
        var lastEnv = definition.Environments.Last();  // No need for .LastOrDefault() ?? fallback
        
        stages.AppendLine($"""
- stage: Rollback
  displayName: 'Rollback on failure'
  dependsOn: Deploy_{lastEnv}
  condition: failed()
  jobs:
  - job: RollbackJob
    steps:
{Yaml.YamlBuilder.Indent(string.Join(Environment.NewLine, 
    rollbackService.GenerateRollbackSteps(definition.Rollback)), 6)}
""");
    }

    private void AppendNotificationStage(StringBuilder stages, PipelineDefinition definition)
    {
        if (definition.Notifications.Count == 0) return;
        
        // ✅ SAFE: Guaranteed to have environment
        var lastEnv = definition.Environments.Last();
        var steps = string.Join(Environment.NewLine, _notificationGenerator.GenerateSteps(definition));
        
        stages.AppendLine($"""
- stage: Notify
  displayName: 'Pipeline notifications'
  dependsOn: Deploy_{lastEnv}
  condition: always()
  jobs:
  - job: Notify
    steps:
{Yaml.YamlBuilder.Indent(steps, 6)}
""");
    }
}
```

---

## Issue G: Unsafe LINQ Chains

### Current Code (❌ WRONG)
```csharp
public sealed class DeploymentStageGenerator : IStageGenerator
{
    public string Generate(PipelineDefinition definition)
    {
        var sb = new StringBuilder();
        var previousStage = "Artifact";
        
        foreach (var env in definition.Environments)
        {
            var stageName = $"Deploy_{env}";
            var steps = new StringBuilder();
            
            steps.AppendLine(string.Join(Environment.NewLine, 
                _artifactService.GenerateDownloadSteps(definition.Artifact, env)));

            if (definition.Rollback.Enabled)
                steps.AppendLine(string.Join(Environment.NewLine, 
                    _rollbackService.GenerateBackupSteps(definition.Rollback, env)));

            // ❌ UNSAFE: If HealthChecks is null, throws NullReferenceException
            foreach (var hc in definition.HealthChecks.Where(h => h.Enabled))
                steps.AppendLine(string.Join(Environment.NewLine, 
                    _healthCheckService.GenerateHealthCheckSteps(hc)));

            // ❌ UNSAFE: If VariableGroups is null, throws NullReferenceException
            var envGroups = definition.VariableGroups
                .Where(g => g.Scope == Enums.VariableGroupScope.Environment && g.EnvironmentName == env)
                .ToList();
            
            var stageVars = envGroups.Count > 0
                ? $"  variables:\n{_variableGroupService.GenerateStageVariables(envGroups)}"
                : string.Empty;

            // ... rest of method
        }
        
        return sb.ToString().TrimEnd();
    }
}
```

### Fixed Code (✅ CORRECT)
```csharp
public sealed class DeploymentStageGenerator : IStageGenerator
{
    public string Generate(PipelineDefinition definition)
    {
        var sb = new StringBuilder();
        var previousStage = "Artifact";
        
        foreach (var env in definition.Environments ?? Array.Empty<string>())
        {
            var stageName = $"Deploy_{env}";
            var steps = new StringBuilder();
            
            steps.AppendLine(string.Join(Environment.NewLine, 
                _artifactService.GenerateDownloadSteps(definition.Artifact, env)));

            if (definition.Rollback?.Enabled ?? false)
                steps.AppendLine(string.Join(Environment.NewLine, 
                    _rollbackService.GenerateBackupSteps(definition.Rollback, env)));

            // ✅ SAFE: Use null-coalescing and null-conditional
            foreach (var hc in (definition.HealthChecks ?? Array.Empty<HealthCheckConfig>())
                .Where(h => h?.Enabled ?? false))
            {
                steps.AppendLine(string.Join(Environment.NewLine, 
                    _healthCheckService.GenerateHealthCheckSteps(hc)));
            }

            // ✅ SAFE: Handle null collections
            var envGroups = (definition.VariableGroups ?? Array.Empty<VariableGroupConfig>())
                .Where(g => g?.Scope == Enums.VariableGroupScope.Environment && g?.EnvironmentName == env)
                .ToList();
            
            var stageVars = envGroups.Count > 0
                ? $"  variables:\n{_variableGroupService.GenerateStageVariables(envGroups)}"
                : string.Empty;

            // ... rest of method
        }
        
        return sb.ToString().TrimEnd();
    }
}
```

### Better: Extension Methods for Safety
```csharp
/// <summary>
/// Extensions to safely handle null collections and items
/// </summary>
public static class SafeLinqExtensions
{
    /// <summary>
    /// Returns the collection if not null, otherwise empty array
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? Array.Empty<T>();
    }

    /// <summary>
    /// Safely checks if item matches predicate, handling null items
    /// </summary>
    public static IEnumerable<T> WhereSafe<T>(this IEnumerable<T> source, Func<T?, bool> predicate)
    {
        return source.Where(item => 
        {
            try
            {
                return item != null && predicate(item);
            }
            catch (Exception ex)
            {
                // Log warning about filter failure
                return false;
            }
        });
    }
}

// Usage:
foreach (var hc in definition.HealthChecks
    .OrEmpty()
    .WhereSafe(h => h.Enabled))
{
    steps.AppendLine(string.Join(Environment.NewLine, 
        _healthCheckService.GenerateHealthCheckSteps(hc)));
}
```

---

## Issue H: Missing File Operation Error Handling

### Current Code (❌ WRONG)
```csharp
public sealed class RepoScannerService : IRepoScannerService
{
    public RepoScanResult ScanDirectory(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return new RepoScanResult { ProjectType = ProjectType.Unknown };

        // ❌ No error handling - can throw:
        // - UnauthorizedAccessException
        // - PathTooLongException  
        // - IOException
        // - OutOfMemoryException (if repo is huge)
        var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(rootPath, f).Replace('\\', '/'))
            .ToList();
        
        return ScanFileList(files);
    }

    public RepoScanResult ScanFileList(IReadOnlyList<string> relativePaths)
    {
        var builder = new RepoScanResultBuilder();
        
        foreach (var path in relativePaths)
        {
            builder.DetectedFiles.Add(path);
            
            foreach (var (pattern, apply) in Rules)
            {
                if (MatchesPattern(path, pattern))
                    apply(builder);
            }
        }

        // ... rest of method
    }
}
```

### Fixed Code (✅ CORRECT)
```csharp
public sealed class RepoScanResult
{
    public ProjectType ProjectType { get; set; } = ProjectType.Unknown;
    public bool HasDockerfile { get; set; }
    public bool HasTests { get; set; }
    public bool HasTerraform { get; set; }
    public bool HasBicep { get; set; }
    public IReadOnlyList<string> SuggestedTemplates { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedFiles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();  // ADD THIS
}

public sealed class RepoScannerService : IRepoScannerService
{
    private const int MaxFilesForScan = 10000;
    private const int MaxScanTimeoutSeconds = 30;

    private readonly ILogger<RepoScannerService> _logger;

    public RepoScannerService(ILogger<RepoScannerService> logger)
    {
        _logger = logger;
    }

    public RepoScanResult ScanDirectory(string rootPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return new RepoScanResult 
                { 
                    ProjectType = ProjectType.Unknown,
                    Errors = new[] { "Repository path cannot be empty" }
                };
            }

            if (!Directory.Exists(rootPath))
            {
                return new RepoScanResult 
                { 
                    ProjectType = ProjectType.Unknown,
                    Errors = new[] { $"Directory not found: {rootPath}" }
                };
            }

            _logger.LogInformation("Scanning directory: {DirectoryPath}", rootPath);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(MaxScanTimeoutSeconds));
            
            var files = new List<string>();
            var fileCount = 0;
            var directoriesScanned = 0;
            
            foreach (var file in Directory.EnumerateFiles(
                rootPath, 
                "*.*", 
                SearchOption.AllDirectories))
            {
                if (cts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Scan operation timed out after {TimeoutSeconds}s", MaxScanTimeoutSeconds);
                    return new RepoScanResult
                    {
                        ProjectType = ProjectType.Unknown,
                        Errors = new[] { $"Scan operation timed out after {MaxScanTimeoutSeconds} seconds" },
                        DetectedFiles = files
                    };
                }

                if (fileCount++ > MaxFilesForScan)
                {
                    _logger.LogWarning(
                        "Repository contains more than {MaxFiles} files. Stopping scan.",
                        MaxFilesForScan);
                    
                    return new RepoScanResult
                    {
                        ProjectType = ProjectType.Unknown,
                        Errors = new[] 
                        { 
                            $"Repository exceeds scan limit of {MaxFilesForScan} files. " +
                            $"Consider using .gitignore to exclude build/node_modules directories."
                        },
                        DetectedFiles = files
                    };
                }

                try
                {
                    files.Add(Path.GetRelativePath(rootPath, file).Replace('\\', '/'));
                }
                catch (PathTooLongException ex)
                {
                    _logger.LogWarning("Path too long, skipping: {FilePath}", file);
                    continue;
                }
            }

            _logger.LogInformation(
                "Scan complete: {FileCount} files found in {DirectoryCount} directories",
                fileCount, directoriesScanned);
            
            return ScanFileList(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied scanning directory: {DirectoryPath}", rootPath);
            return new RepoScanResult
            {
                ProjectType = ProjectType.Unknown,
                Errors = new[] 
                { 
                    $"Access denied when scanning {rootPath}. " +
                    $"Please check file permissions. Error: {ex.Message}"
                }
            };
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found: {DirectoryPath}", rootPath);
            return new RepoScanResult
            {
                ProjectType = ProjectType.Unknown,
                Errors = new[] { $"Directory not found: {rootPath}" }
            };
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error scanning directory: {DirectoryPath}", rootPath);
            return new RepoScanResult
            {
                ProjectType = ProjectType.Unknown,
                Errors = new[] 
                { 
                    $"Error reading directory {rootPath}. " +
                    $"The directory may be locked or in use. Error: {ex.Message}"
                }
            };
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Scan operation cancelled for: {DirectoryPath}", rootPath);
            return new RepoScanResult
            {
                ProjectType = ProjectType.Unknown,
                Errors = new[] { "Scan operation was cancelled" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning directory: {DirectoryPath}", rootPath);
            return new RepoScanResult
            {
                ProjectType = ProjectType.Unknown,
                Errors = new[] 
                { 
                    $"Unexpected error scanning repository: {ex.Message}. " +
                    $"Please check the path and try again."
                }
            };
        }
    }

    public RepoScanResult ScanFileList(IReadOnlyList<string> relativePaths)
    {
        try
        {
            if (relativePaths == null || relativePaths.Count == 0)
            {
                return new RepoScanResult { ProjectType = ProjectType.Unknown };
            }

            var builder = new RepoScanResultBuilder();

            foreach (var path in relativePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                builder.DetectedFiles.Add(path);

                foreach (var (pattern, apply) in Rules)
                {
                    try
                    {
                        if (MatchesPattern(path, pattern))
                            apply(builder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Error applying pattern {Pattern} to path {Path}",
                            pattern, path);
                        continue;
                    }
                }
            }

            builder.ProjectType ??= builder.HasDotNet ? ProjectType.DotNet
                : builder.HasDockerfile ? ProjectType.Docker
                : builder.HasTerraform ? ProjectType.Terraform
                : ProjectType.Unknown;

            if (builder.HasDockerfile && builder.HasDotNet)
                builder.Suggest("hybrid-dotnet-docker");

            return builder.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning file list");
            return new RepoScanResult
            {
                ProjectType = ProjectType.Unknown,
                Errors = new[] { $"Error analyzing files: {ex.Message}" }
            };
        }
    }
}
```

---

## Issue P: No Input Validation

### Current Code (❌ WRONG)
```csharp
public sealed class PipelineDefinition
{
    public string Name { get; set; } = "enterprise-pipeline";  // ❌ Can be set to anything
    public string? PoolName { get; set; }  // ❌ No validation
    public IReadOnlyList<string> Environments { get; set; } = new[] { "test", "preprod", "prod" };  // ❌ Can be null
}

public sealed class VariableGroupConfig
{
    public string Name { get; set; } = string.Empty;  // ❌ Can be empty
    public VariableGroupScope Scope { get; set; }  // ❌ No validation
}

public sealed class ArtifactConfig
{
    public ArtifactType ArtifactType { get; set; }  // ❌ No validation
    public string ArtifactName { get; set; } = "drop";  // ❌ Can be empty
    public string? PackagePath { get; set; }  // ❌ No path validation
}
```

### Fixed Code (✅ CORRECT)
```csharp
public sealed class PipelineDefinition
{
    private string _name = "enterprise-pipeline";
    private IReadOnlyList<string> _environments = new[] { "test", "preprod", "prod" };

    /// <summary>
    /// Gets or sets the pipeline name. Must be 1-255 alphanumeric characters with dashes/underscores.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            ValidatePipelineName(value);
            _name = value;
        }
    }

    public ProjectType ProjectType { get; set; }
    public BuildAgentType BuildAgent { get; set; } = BuildAgentType.MicrosoftHosted;

    private string? _poolName;
    public string? PoolName
    {
        get => _poolName;
        set
        {
            if (value != null)
                ValidatePoolName(value);
            _poolName = value;
        }
    }

    /// <summary>
    /// Gets or sets deployment environments. Must have at least one.
    /// </summary>
    public IReadOnlyList<string> Environments
    {
        get => _environments;
        set
        {
            ValidateEnvironments(value);
            _environments = value;
        }
    }

    public IReadOnlyList<VariableGroupConfig> VariableGroups { get; set; } = Array.Empty<VariableGroupConfig>();
    public KeyVaultConfig? KeyVault { get; set; }
    public ArtifactConfig Artifact { get; set; } = new();
    public RollbackConfig Rollback { get; set; } = new();
    public IReadOnlyList<HealthCheckConfig> HealthChecks { get; set; } = Array.Empty<HealthCheckConfig>();
    public IReadOnlyList<NotificationConfig> Notifications { get; set; } = Array.Empty<NotificationConfig>();
    public InfrastructureAsCodeConfig? IaC { get; set; }
    public DeploymentStrategyConfig DeploymentStrategy { get; set; } = new();
    public GovernancePolicyConfig Governance { get; set; } = new();
    public AgentDiagnosticConfig? AgentDiagnostics { get; set; }
    public IReadOnlyList<StageDependency> StageDependencies { get; set; } = Array.Empty<StageDependency>();
    public string? DotNetProjectPath { get; set; }
    public string? SolutionPath { get; set; }

    private static void ValidatePipelineName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipeline name is required and cannot be empty", nameof(Name));

        if (name.Length > 255)
            throw new ArgumentException("Pipeline name cannot exceed 255 characters", nameof(Name));

        if (name.Length < 3)
            throw new ArgumentException("Pipeline name must be at least 3 characters", nameof(Name));

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_-]+$"))
            throw new ArgumentException(
                "Pipeline name can only contain letters, numbers, dashes, and underscores",
                nameof(Name));
    }

    private static void ValidatePoolName(string poolName)
    {
        if (string.IsNullOrWhiteSpace(poolName))
            throw new ArgumentException("Pool name cannot be empty", nameof(PoolName));

        if (poolName.Length > 255)
            throw new ArgumentException("Pool name cannot exceed 255 characters", nameof(PoolName));

        if (!Regex.IsMatch(poolName, @"^[a-zA-Z0-9_.-]+$"))
            throw new ArgumentException(
                "Pool name contains invalid characters",
                nameof(PoolName));
    }

    private static void ValidateEnvironments(IReadOnlyList<string>? environments)
    {
        if (environments == null || environments.Count == 0)
            throw new ArgumentException(
                "At least one environment must be defined (e.g., test, preprod, prod)",
                nameof(Environments));

        if (environments.Count > 50)
            throw new ArgumentException(
                "Cannot define more than 50 environments",
                nameof(Environments));

        foreach (var env in environments)
        {
            if (string.IsNullOrWhiteSpace(env))
                throw new ArgumentException("Environment names cannot be empty", nameof(Environments));

            if (env.Length > 50)
                throw new ArgumentException(
                    $"Environment name '{env}' exceeds maximum length of 50 characters",
                    nameof(Environments));

            if (!Regex.IsMatch(env, @"^[a-zA-Z0-9_-]+$"))
                throw new ArgumentException(
                    $"Environment name '{env}' contains invalid characters. Use only alphanumeric, dash, and underscore.",
                    nameof(Environments));
        }
    }
}

public sealed class VariableGroupConfig
{
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the variable group name. Required and must be non-empty.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Variable group name is required",
                    nameof(Name));

            if (value.Length > 255)
                throw new ArgumentException(
                    "Variable group name cannot exceed 255 characters",
                    nameof(Name));

            _name = value;
        }
    }

    public VariableGroupScope Scope { get; set; } = VariableGroupScope.Pipeline;
    public string? EnvironmentName { get; set; }
    public bool ContainsSecrets { get; set; }
}

public sealed class ArtifactConfig
{
    private string _artifactName = "drop";

    public ArtifactType ArtifactType { get; set; } = ArtifactType.PipelineArtifact;

    /// <summary>
    /// Gets or sets the artifact name. Required, 1-255 characters.
    /// </summary>
    public string ArtifactName
    {
        get => _artifactName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Artifact name is required", nameof(ArtifactName));

            if (value.Length > 255)
                throw new ArgumentException("Artifact name cannot exceed 255 characters", nameof(ArtifactName));

            if (!Regex.IsMatch(value, @"^[a-zA-Z0-9_.-]+$"))
                throw new ArgumentException(
                    "Artifact name contains invalid characters",
                    nameof(ArtifactName));

            _artifactName = value;
        }
    }

    public string? PackagePath { get; set; }
    public string? PublishPath { get; set; }
    public string? DownloadPath { get; set; }
}
```

---

## Issue B: Duplicate Pool Configuration

### Current Code (❌ WRONG)
```csharp
// BuildStageGenerator.cs
public string Generate(PipelineDefinition definition)
{
    // ❌ DUPLICATED CODE:
    var pool = definition.BuildAgent == BuildAgentType.SelfHosted
        ? $"name: '{definition.PoolName ?? "Default"}'"
        : "vmImage: 'windows-latest'";
    // ... rest of generation
}

// TestStageGenerator.cs  
public string Generate(PipelineDefinition definition)
{
    // ❌ EXACT SAME CODE:
    var pool = definition.BuildAgent == BuildAgentType.SelfHosted
        ? $"name: '{definition.PoolName ?? "Default"}'"
        : "vmImage: 'windows-latest'";
    // ... rest of generation
}
```

### Fixed Code (✅ CORRECT)
```csharp
// New file: PoolConfigurationHelper.cs
public static class PoolConfigurationHelper
{
    /// <summary>
    /// Generates YAML pool configuration based on build agent type
    /// </summary>
    public static string GeneratePoolConfiguration(BuildAgentType buildAgent, string? poolName = null)
    {
        return buildAgent == BuildAgentType.SelfHosted
            ? $"name: '{poolName ?? "Default"}'"
            : "vmImage: 'windows-latest'";
    }

    /// <summary>
    /// Gets the default pool name for a given agent type
    /// </summary>
    public static string GetDefaultPoolName(BuildAgentType buildAgent)
    {
        return buildAgent switch
        {
            BuildAgentType.SelfHosted => "Default",
            BuildAgentType.MicrosoftHosted => "windows-latest",
            _ => throw new ArgumentException($"Unknown agent type: {buildAgent}")
        };
    }
}

// BuildStageGenerator.cs - UPDATED
public sealed class BuildStageGenerator : IStageGenerator
{
    public string StageName => "Build";

    public string Generate(PipelineDefinition definition)
    {
        // ✅ USE HELPER:
        var pool = PoolConfigurationHelper.GeneratePoolConfiguration(
            definition.BuildAgent, 
            definition.PoolName);

        var steps = new StringBuilder();
        if (definition.ProjectType == ProjectType.DotNet)
        {
            var project = definition.DotNetProjectPath ?? "**/*.csproj";
            steps.AppendLine(YamlBuilder.Task("DotNetCoreCLI@2", new Dictionary<string, string>
            {
                ["command"] = "restore",
                ["projects"] = project
            }, "Restore NuGet packages"));
            steps.AppendLine();
            steps.AppendLine(YamlBuilder.Task("DotNetCoreCLI@2", new Dictionary<string, string>
            {
                ["command"] = "build",
                ["projects"] = project,
                ["arguments"] = "--configuration $(BuildConfiguration) --no-restore"
            }, "Build solution"));
        }
        else
        {
            steps.AppendLine(YamlBuilder.PowerShellStep(
                "Write-Host 'Build steps configured for project type: " + definition.ProjectType + "'",
                "Build placeholder"));
        }

        return $"""
- stage: Build
  displayName: 'Build'
  jobs:
  - job: BuildJob
    displayName: 'Compile and package'
    pool:
      {pool}
    steps:
{YamlBuilder.Indent(steps.ToString(), 6)}
""";
    }
}

// TestStageGenerator.cs - UPDATED
public sealed class TestStageGenerator : IStageGenerator
{
    public string StageName => "Test";

    public string Generate(PipelineDefinition definition)
    {
        // ✅ USE HELPER:
        var pool = PoolConfigurationHelper.GeneratePoolConfiguration(
            definition.BuildAgent, 
            definition.PoolName);

        var steps = new StringBuilder();
        if (definition.ProjectType == ProjectType.DotNet)
        {
            steps.AppendLine(YamlBuilder.Task("DotNetCoreCLI@2", new Dictionary<string, string>
            {
                ["command"] = "test",
                ["projects"] = definition.SolutionPath ?? "**/*Tests*.csproj",
                ["arguments"] = "--configuration $(BuildConfiguration) --no-build --collect:\"XPlat Code Coverage\""
            }, "Run unit tests"));
        }
        else
        {
            steps.AppendLine(YamlBuilder.PowerShellStep("Write-Host 'No test runner configured'", "Test placeholder"));
        }

        return $"""
- stage: Test
  displayName: 'Test'
  dependsOn: Build
  condition: succeeded()
  jobs:
  - job: TestJob
    displayName: 'Run tests'
    pool:
      {pool}
    steps:
{YamlBuilder.Indent(steps.ToString(), 6)}
""";
    }
}
```

---

## Testing the Fixes

### Test Cases for Issue F (Null Safety)
```csharp
[TestClass]
public class PipelineGeneratorServiceTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyEnvironments_ThrowsArgumentException()
    {
        var definition = new PipelineDefinition
        {
            Name = "test",
            Environments = Array.Empty<string>()  // ❌ Should throw
        };
        
        var service = CreateService();
        service.Generate(definition);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithNullEnvironments_ThrowsArgumentException()
    {
        var definition = new PipelineDefinition();
        definition.Environments = null;  // ❌ Should throw
        
        var service = CreateService();
        service.Generate(definition);
    }

    [TestMethod]
    public void Generate_WithValidEnvironments_Succeeds()
    {
        var definition = new PipelineDefinition
        {
            Name = "test-pipeline",
            ProjectType = ProjectType.DotNet,
            Environments = new[] { "test", "prod" }  // ✅ Valid
        };
        
        var service = CreateService();
        var result = service.Generate(definition);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Yaml.Contains("Deploy_test"));
        Assert.IsTrue(result.Yaml.Contains("Deploy_prod"));
    }
}
```

