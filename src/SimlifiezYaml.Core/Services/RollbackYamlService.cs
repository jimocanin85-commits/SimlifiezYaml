using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;

namespace SimlifiezYaml.Core.Services;

public sealed class RollbackYamlService : IRollbackYamlService
{
    public IReadOnlyList<string> GenerateBackupSteps(RollbackConfig config, string environment)
    {
        if (!config.Enabled) return Array.Empty<string>();

        var backupPath = config.BackupPath ?? $"D:\\backups\\{environment}\\$(Build.BuildId)";

        return config.Target switch
        {
            RollbackTarget.Iis => new[]
            {
                YamlBuilder.PowerShellStep($"""
$backupRoot = {YamlBuilder.PsLiteral(backupPath)}
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
$sitePath = 'C:\inetpub\wwwroot'
Copy-Item -Path $sitePath -Destination $backupRoot -Recurse -Force
Write-Host "IIS backup created at $backupRoot"
""", "Backup IIS site before deploy")
            },
            RollbackTarget.WindowsService => new[]
            {
                YamlBuilder.PowerShellStep($"""
$backupRoot = {YamlBuilder.PsLiteral(backupPath)}
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
Stop-Service -Name 'MyService' -Force -ErrorAction SilentlyContinue
Copy-Item -Path 'C:\Services\MyService' -Destination $backupRoot -Recurse -Force
Write-Host 'Windows Service backup completed'
""", "Backup Windows Service before deploy")
            },
            RollbackTarget.FileShare => new[]
            {
                YamlBuilder.PowerShellStep($"""
$backupRoot = {YamlBuilder.PsLiteral(backupPath)}
robocopy '\\fileserver\deploy' $backupRoot /MIR /R:2 /W:5
if ($LASTEXITCODE -ge 8) {{ throw 'File share backup failed' }}
""", "Backup file share deployment")
            },
            RollbackTarget.AzureAppServiceSlot => new[]
            {
                YamlBuilder.Task("AzureAppServiceManage@0", new Dictionary<string, string>
                {
                    ["Action"] = "Swap Slots",
                    ["WebAppName"] = "$(WEBAPP_NAME)",
                    ["ResourceGroupName"] = "$(RESOURCE_GROUP)",
                    ["SourceSlot"] = "production",
                    ["SwapWithProduction"] = "false"
                }, "Prepare App Service slot for rollback")
            },
            RollbackTarget.DockerContainer => new[]
            {
                YamlBuilder.PowerShellStep($"""
docker tag $(IMAGE_NAME):latest $(IMAGE_NAME):backup-$(Build.BuildId)
Write-Host 'Docker image tagged for rollback'
""", "Tag Docker image for rollback")
            },
            _ => Array.Empty<string>()
        };
    }

    public IReadOnlyList<string> GenerateRollbackSteps(RollbackConfig config)
    {
        if (!config.Enabled) return Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(config.RollbackScript))
            return new[] { YamlBuilder.ScriptStep(config.RollbackScript, "Execute custom rollback script") };

        return config.Target switch
        {
            RollbackTarget.Iis => new[]
            {
                YamlBuilder.PowerShellStep($"""
$backupRoot = {YamlBuilder.PsLiteral(config.BackupPath ?? "D:\\backups")}
$latest = Get-ChildItem $backupRoot | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $latest) {{ throw 'No backup found for rollback' }}
Copy-Item -Path $latest.FullName -Destination 'C:\inetpub\wwwroot' -Recurse -Force
Write-Host 'IIS rollback completed'
""", "Rollback IIS deployment")
            },
            RollbackTarget.AzureAppServiceSlot => new[]
            {
                YamlBuilder.Task("AzureAppServiceManage@0", new Dictionary<string, string>
                {
                    ["Action"] = "Swap Slots",
                    ["WebAppName"] = "$(WEBAPP_NAME)",
                    ["ResourceGroupName"] = "$(RESOURCE_GROUP)",
                    ["SourceSlot"] = "staging",
                    ["SwapWithProduction"] = "true"
                }, "Rollback via App Service slot swap")
            },
            RollbackTarget.DockerContainer => new[]
            {
                YamlBuilder.PowerShellStep(
                    "docker pull $(IMAGE_NAME):backup-$(Build.BuildId); docker tag $(IMAGE_NAME):backup-$(Build.BuildId) $(IMAGE_NAME):latest",
                    "Rollback Docker container")
            },
            _ => new[]
            {
                YamlBuilder.PowerShellStep("Write-Warning 'Configure RollbackScript for this target type'", "Rollback placeholder")
            }
        };
    }
}
