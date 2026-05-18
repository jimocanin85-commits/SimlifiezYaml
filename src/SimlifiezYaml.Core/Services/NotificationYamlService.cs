using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;

namespace SimlifiezYaml.Core.Services;

public sealed class NotificationYamlService : INotificationYamlService
{
    public IReadOnlyList<string> GenerateNotificationSteps(NotificationConfig config, bool succeeded)
    {
        var condition = succeeded ? "succeeded()" : "failed()";
        return config.NotificationType switch
        {
            NotificationType.TeamsWebhook => new[]
            {
                YamlBuilder.PowerShellStep($"""
# condition: {condition}
$webhook = $env:TEAMS_WEBHOOK ?? '$({config.TeamsWebhookVariable ?? "TEAMS_WEBHOOK_URL")})'
if ([string]::IsNullOrWhiteSpace($webhook)) {{ Write-Warning 'Teams webhook variable not set'; exit 0 }}
$body = @{ text = "Pipeline $(Build.DefinitionName) #$(Build.BuildNumber) - {(succeeded ? "succeeded" : "failed")}" } | ConvertTo-Json
Invoke-RestMethod -Uri $webhook -Method Post -Body $body -ContentType 'application/json'
""", succeeded ? "Notify Teams on success" : "Notify Teams on failure")
            },
            NotificationType.Email => new[]
            {
                YamlBuilder.PowerShellStep($"""
# Email placeholder - configure SendGrid or SMTP extension
# Recipients: {string.Join(", ", config.EmailRecipients)}
Write-Host 'Email notification would be sent to configured recipients via pipeline variables'
""", "Email notification placeholder")
            },
            NotificationType.CustomWebhook => new[]
            {
                YamlBuilder.PowerShellStep($"""
$webhook = '$({config.WebhookUrlVariable ?? "CUSTOM_WEBHOOK_URL")})'
$payload = @{{ event = '{(succeeded ? "success" : "failure")}'; build = '$(Build.BuildNumber)' }} | ConvertTo-Json
Invoke-RestMethod -Uri $webhook -Method Post -Body $payload -ContentType 'application/json'
""", "Custom webhook notification")
            },
            _ => Array.Empty<string>()
        };
    }
}
