using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;
using SimlifiezYaml.Core.Yaml;

namespace SimlifiezYaml.Core.Services;

/// <summary>
/// Generates notification steps. Whether a step runs on success or failure is decided by the
/// stage it is placed in (see <see cref="PipelineYamlAssembler.AddNotificationStages"/>), not by
/// the step itself.
/// </summary>
public sealed class NotificationYamlService : INotificationYamlService
{
    public IReadOnlyList<string> GenerateNotificationSteps(NotificationConfig config, bool succeeded)
    {
        var outcome = succeeded ? "succeeded" : "failed";
        return config.NotificationType switch
        {
            NotificationType.TeamsWebhook => new[]
            {
                YamlBuilder.PowerShellStep($$"""
$webhook = {{WebhookMacro(config.TeamsWebhookVariable, "TEAMS_WEBHOOK_URL")}}
if ([string]::IsNullOrWhiteSpace($webhook) -or $webhook.StartsWith('$(')) {
  Write-Warning 'Teams webhook variable is not set; skipping notification.'
  exit 0
}
$body = @{ text = "Pipeline $(Build.DefinitionName) #$(Build.BuildNumber) {{outcome}}" } | ConvertTo-Json
Invoke-RestMethod -Uri $webhook -Method Post -Body $body -ContentType 'application/json'
""", succeeded ? "Notify Teams on success" : "Notify Teams on failure")
            },
            NotificationType.Email => new[]
            {
                YamlBuilder.PowerShellStep($$"""
# Email placeholder - configure SendGrid or an SMTP extension.
# Recipients: {{string.Join(", ", config.EmailRecipients).ReplaceLineEndings(" ")}}
Write-Host 'Email notification ({{outcome}}) would be sent to the configured recipients.'
""", succeeded ? "Email notification on success" : "Email notification on failure")
            },
            NotificationType.CustomWebhook => new[]
            {
                YamlBuilder.PowerShellStep($$"""
$webhook = {{WebhookMacro(config.WebhookUrlVariable, "CUSTOM_WEBHOOK_URL")}}
if ([string]::IsNullOrWhiteSpace($webhook) -or $webhook.StartsWith('$(')) {
  Write-Warning 'Webhook variable is not set; skipping notification.'
  exit 0
}
$payload = @{ event = '{{outcome}}'; build = '$(Build.BuildNumber)' } | ConvertTo-Json
Invoke-RestMethod -Uri $webhook -Method Post -Body $payload -ContentType 'application/json'
""", succeeded ? "Custom webhook on success" : "Custom webhook on failure")
            },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Builds a PowerShell string literal holding an Azure DevOps macro such as
    /// <c>'$(TEAMS_WEBHOOK_URL)'</c>, which the agent replaces with the variable's value at runtime.
    /// Invalid variable names fall back to the default name.
    /// </summary>
    private static string WebhookMacro(string? variableName, string fallback)
    {
        var name = string.IsNullOrWhiteSpace(variableName) || !IsValidVariableName(variableName)
            ? fallback
            : variableName.Trim();
        return YamlBuilder.PsLiteral($"$({name})");
    }

    private static bool IsValidVariableName(string name) =>
        name.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-');
}
