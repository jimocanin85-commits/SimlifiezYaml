using System.Text;

namespace SimlifiezYaml.Core.Yaml;

/// <summary>
/// YAML builder with proper escaping for PowerShell scripts and YAML string values.
/// Prevents script injection and YAML syntax errors.
/// </summary>
public static class YamlBuilder
{
    public static string Indent(string content, int spaces = 2)
    {
        var pad = new string(' ', spaces);
        var lines = content.Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
                continue;
            }
            sb.Append(pad).AppendLine(line.TrimEnd('\r'));
        }
        return sb.ToString().TrimEnd();
    }

    public static string Task(string taskName, IDictionary<string, string> inputs, string? displayName = null, int indent = 4)
    {
        var pad = new string(' ', indent);
        var sb = new StringBuilder();
        sb.Append(pad).Append("- task: ").AppendLine(taskName);
        if (!string.IsNullOrEmpty(displayName))
            sb.Append(pad).Append("  displayName: '").Append(EscapeYamlString(displayName)).AppendLine("'");
        sb.Append(pad).AppendLine("  inputs:");
        foreach (var (key, value) in inputs)
            sb.Append(pad).Append("    ").Append(key).Append(": '").Append(EscapeYamlString(value)).AppendLine("'");
        return sb.ToString().TrimEnd();
    }

    public static string PowerShellStep(string script, string displayName, int indent = 4)
    {
        var pad = new string(' ', indent);
        var escaped = EscapePowerShellString(script);
        var escapedName = EscapeYamlString(displayName);
        return $"""
{pad}- powershell: |
{pad}    {escaped.Replace("\n", "\n" + pad + "    ")}
{pad}  displayName: '{escapedName}'
""".TrimEnd();
    }

    public static string ScriptStep(string scriptPath, string displayName, int indent = 4)
    {
        var pad = new string(' ', indent);
        var escapedPath = EscapeYamlString(scriptPath);
        var escapedName = EscapeYamlString(displayName);
        return $"{pad}- script: {escapedPath}\n{pad}  displayName: '{escapedName}'";
    }

    /// <summary>
    /// Escapes a string for use in YAML single-quoted string values.
    /// </summary>
    private static string EscapeYamlString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;
        
        // In YAML, single quotes are escaped by doubling them
        return value.Replace("'", "''").Replace("\\", "\\\\");
    }

    /// <summary>
    /// Escapes a string for safe use in PowerShell inline scripts.
    /// </summary>
    private static string EscapePowerShellString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;
        
        // Escape for PowerShell literal strings (single-quoted)
        // Single quotes are escaped by doubling, dollar signs need backtick in some contexts
        return value
            .Replace("'", "''")
            .Replace("$(", "$([")  // Prevent subexpression execution
            .Replace("${", "${"); // Alternative subexpression
    }

    /// <summary>
    /// Legacy escape method for backward compatibility.
    /// </summary>
    private static string Escape(string value) => EscapeYamlString(value);
}
