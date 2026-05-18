using System.Text;

namespace SimlifiezYaml.Core.Yaml;

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
            sb.Append(pad).Append("  displayName: '").Append(displayName).AppendLine("'");
        sb.Append(pad).AppendLine("  inputs:");
        foreach (var (key, value) in inputs)
            sb.Append(pad).Append("    ").Append(key).Append(": '").Append(Escape(value)).AppendLine("'");
        return sb.ToString().TrimEnd();
    }

    public static string PowerShellStep(string script, string displayName, int indent = 4)
    {
        var pad = new string(' ', indent);
        var escaped = script.Replace("'", "''");
        return $"""
{pad}- powershell: |
{pad}    {escaped.Replace("\n", "\n" + pad + "    ")}
{pad}  displayName: '{displayName}'
""".TrimEnd();
    }

    public static string ScriptStep(string scriptPath, string displayName, int indent = 4)
    {
        var pad = new string(' ', indent);
        return $"{pad}- script: {scriptPath}\n{pad}  displayName: '{displayName}'";
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
