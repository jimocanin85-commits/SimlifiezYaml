using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Services;

public sealed class TemplateMarketplaceService : ITemplateMarketplaceService
{
    private static readonly IReadOnlyList<PipelineTemplate> Templates = new List<PipelineTemplate>
    {
        T("dotnet-web-app", ".NET Web Application", TemplateCategory.DotNet, "Build, test, and deploy ASP.NET Core to App Service",
            DeploymentTarget.Cloud, new[] { "projectPath", "webAppName" }, new[] { "Build", "Test", "Artifact", "Deploy_test", "Deploy_prod" }, TemplateRiskLevel.Low, "dotnet", "web"),
        T("iis-onprem", "IIS On-Premises", TemplateCategory.Iis, "Deploy to IIS with backup and health checks",
            DeploymentTarget.OnPrem, new[] { "sitePath", "appPool" }, new[] { "Build", "Artifact", "Deploy_test", "Deploy_prod" }, TemplateRiskLevel.Medium, "iis", "onprem"),
        T("docker-build-push", "Docker Build and Push", TemplateCategory.Docker, "Build container image and push to registry",
            DeploymentTarget.Cloud, new[] { "dockerfile", "registry" }, new[] { "Build", "Artifact" }, TemplateRiskLevel.Low, "docker"),
        T("terraform-azure", "Terraform Azure", TemplateCategory.Terraform, "Plan and apply Terraform for Azure resources",
            DeploymentTarget.Cloud, new[] { "workingDirectory", "serviceConnection" }, new[] { "Build", "Deploy_test" }, TemplateRiskLevel.High, "terraform", "iac"),
        T("winrm-deploy", "WinRM Deployment", TemplateCategory.WinRm, "Deploy via WinRM to Windows servers",
            DeploymentTarget.OnPrem, new[] { "targetHost", "credentialVariable" }, new[] { "Artifact", "Deploy_test", "Deploy_prod" }, TemplateRiskLevel.High, "winrm"),
        T("aks-deploy", "AKS Deployment", TemplateCategory.Aks, "Deploy manifests to Azure Kubernetes Service",
            DeploymentTarget.Cloud, new[] { "cluster", "namespace" }, new[] { "Build", "Artifact", "Deploy_test", "Deploy_prod" }, TemplateRiskLevel.High, "kubernetes", "aks"),
        T("hybrid-dotnet-docker", "Hybrid .NET + Docker", TemplateCategory.Hybrid, "Build .NET app, containerize, deploy to AKS or App Service",
            DeploymentTarget.Hybrid, new[] { "projectPath", "dockerfile" }, new[] { "Build", "Test", "Artifact", "Deploy_test", "Deploy_preprod", "Deploy_prod" }, TemplateRiskLevel.Medium, "hybrid")
    };

    public IReadOnlyList<PipelineTemplate> GetAllTemplates() => Templates;

    public IReadOnlyList<PipelineTemplate> GetByCategory(TemplateCategory category) =>
        Templates.Where(t => t.Category == category).ToList();

    public PipelineTemplate? GetById(string id) =>
        Templates.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static PipelineTemplate T(
        string id, string name, TemplateCategory category, string description,
        DeploymentTarget target, string[] inputs, string[] stages,
        TemplateRiskLevel risk, params string[] tags) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Description = description,
        SupportedTargets = new[] { target },
        RequiredInputs = inputs,
        GeneratedStages = stages,
        RiskLevel = risk,
        Tags = tags
    };
}
