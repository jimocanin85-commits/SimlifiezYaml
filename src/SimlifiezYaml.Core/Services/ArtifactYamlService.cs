using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;

namespace SimlifiezYaml.Core.Services;

public sealed class ArtifactYamlService : IArtifactYamlService
{
    public IReadOnlyList<string> GeneratePublishSteps(ArtifactConfig config)
    {
        return config.ArtifactType switch
        {
            ArtifactType.PipelineArtifact => new[]
            {
                YamlBuilder.Task("PublishPipelineArtifact@1", new Dictionary<string, string>
                {
                    ["targetPath"] = config.PublishPath ?? "$(Build.ArtifactStagingDirectory)",
                    ["artifactName"] = config.ArtifactName,
                    ["publishLocation"] = "pipeline"
                }, $"Publish pipeline artifact: {config.ArtifactName}")
            },
            ArtifactType.BuildArtifact => new[]
            {
                YamlBuilder.Task("PublishBuildArtifacts@1", new Dictionary<string, string>
                {
                    ["PathtoPublish"] = config.PublishPath ?? "$(Build.ArtifactStagingDirectory)",
                    ["ArtifactName"] = config.ArtifactName,
                    ["publishLocation"] = "Container"
                }, $"Publish build artifact: {config.ArtifactName}")
            },
            ArtifactType.ZipPackage => new[]
            {
                YamlBuilder.Task("ArchiveFiles@2", new Dictionary<string, string>
                {
                    ["rootFolderOrFile"] = config.PackagePath ?? "$(Build.ArtifactStagingDirectory)",
                    ["includeRootFolder"] = "false",
                    ["archiveType"] = "zip",
                    ["archiveFile"] = $"$(Build.ArtifactStagingDirectory)/{config.ArtifactName}.zip"
                }, "Archive deployment package"),
                YamlBuilder.Task("PublishPipelineArtifact@1", new Dictionary<string, string>
                {
                    ["targetPath"] = $"$(Build.ArtifactStagingDirectory)/{config.ArtifactName}.zip",
                    ["artifactName"] = config.ArtifactName,
                    ["publishLocation"] = "pipeline"
                }, "Publish zip artifact")
            },
            ArtifactType.DockerImage => new[]
            {
                YamlBuilder.Task("Docker@2", new Dictionary<string, string>
                {
                    ["command"] = "buildAndPush",
                    ["repository"] = config.ArtifactName,
                    ["dockerfile"] = config.PackagePath ?? "**/Dockerfile",
                    ["containerRegistry"] = "$(DOCKER_SERVICE_CONNECTION)"
                }, "Build and push Docker image")
            },
            ArtifactType.NuGetPackage => new[]
            {
                YamlBuilder.Task("NuGetCommand@2", new Dictionary<string, string>
                {
                    ["command"] = "pack",
                    ["packagesToPack"] = config.PackagePath ?? "**/*.csproj",
                    ["configuration"] = "$(BuildConfiguration)"
                }, "Pack NuGet packages"),
                YamlBuilder.Task("NuGetCommand@2", new Dictionary<string, string>
                {
                    ["command"] = "push",
                    ["packagesToPush"] = "$(Build.ArtifactStagingDirectory)/**/*.nupkg",
                    ["publishVstsFeed"] = "$(NUGET_FEED)"
                }, "Push NuGet packages")
            },
            _ => Array.Empty<string>()
        };
    }

    public IReadOnlyList<string> GenerateDownloadSteps(ArtifactConfig config, string? environment = null)
    {
        var display = environment != null ? $"Download artifact for {environment}" : "Download artifact";
        return config.ArtifactType switch
        {
            ArtifactType.BuildArtifact => new[]
            {
                YamlBuilder.Task("DownloadBuildArtifacts@1", new Dictionary<string, string>
                {
                    ["buildType"] = "current",
                    ["downloadType"] = "single",
                    ["artifactName"] = config.ArtifactName,
                    ["downloadPath"] = config.DownloadPath ?? "$(Pipeline.Workspace)"
                }, display)
            },
            _ => new[]
            {
                YamlBuilder.Task("DownloadPipelineArtifact@2", new Dictionary<string, string>
                {
                    ["artifactName"] = config.ArtifactName,
                    ["targetPath"] = config.DownloadPath ?? "$(Pipeline.Workspace)/drop"
                }, display)
            }
        };
    }
}
