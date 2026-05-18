using System.Text.RegularExpressions;
using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Enums;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Services;

public sealed class SecretsGovernanceService : ISecretsGovernanceService
{
    private static readonly (Regex Pattern, string Label, SecretSeverity Severity)[] SecretPatterns =
    {
        (new Regex(@"password\s*[:=]\s*['""]?[^'""$\s][^'""\n]{3,}", RegexOptions.IgnoreCase), "password", SecretSeverity.Error),
        (new Regex(@"connectionstring\s*[:=]\s*['""]?[^'""$\s]", RegexOptions.IgnoreCase), "connection string", SecretSeverity.Error),
        (new Regex(@"clientsecret\s*[:=]\s*['""]?[^'""$\s]", RegexOptions.IgnoreCase), "client secret", SecretSeverity.Error),
        (new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.IgnoreCase), "private key", SecretSeverity.Error),
        (new Regex(@"(api[_-]?key|token)\s*[:=]\s*['""]?[a-zA-Z0-9]{16,}", RegexOptions.IgnoreCase), "token or API key", SecretSeverity.Warning),
        (new Regex(@"AccountKey=[^;$\s]+", RegexOptions.IgnoreCase), "storage account key", SecretSeverity.Error)
    };

    public IReadOnlyList<SecretGovernanceResult> ScanYaml(string yaml)
    {
        var results = new List<SecretGovernanceResult>();
        var lines = yaml.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Contains("$(") || line.Contains("***"))
                continue;

            foreach (var (pattern, label, severity) in SecretPatterns)
            {
                if (!pattern.IsMatch(line)) continue;
                results.Add(new SecretGovernanceResult
                {
                    HasPlainTextSecret = true,
                    SecretLocation = $"Line {i + 1}: {line.Trim()}",
                    Severity = severity,
                    Recommendation = $"Possible plaintext {label} detected. Use Azure DevOps secret variables, variable groups, or Azure Key Vault instead of embedding values in YAML."
                });
            }
        }
        return results;
    }

    public IReadOnlyList<ValidationResult> ToValidationResults(IReadOnlyList<SecretGovernanceResult> results) =>
        results.Select(r => new ValidationResult
        {
            Severity = r.Severity switch
            {
                SecretSeverity.Error => ValidationSeverity.Error,
                SecretSeverity.Warning => ValidationSeverity.Warning,
                _ => ValidationSeverity.Info
            },
            Message = r.Recommendation,
            AffectedField = r.SecretLocation,
            SuggestedFix = "Store secrets in Azure DevOps Library (variable groups), mark variables as secret, or use AzureKeyVault@2."
        }).ToList();
}
