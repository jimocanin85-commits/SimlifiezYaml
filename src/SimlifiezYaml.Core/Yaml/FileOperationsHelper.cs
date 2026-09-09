using System.Diagnostics;

namespace SimlifiezYaml.Core.Yaml;

/// <summary>
/// Helper class for safe file operations with comprehensive error handling.
/// Provides methods for reading and writing pipeline YAML files with proper exception handling.
/// </summary>
public static class FileOperationsHelper
{
    /// <summary>
    /// Safely writes YAML content to a file with error handling.
    /// </summary>
    /// <param name="filePath">The target file path</param>
    /// <param name="yamlContent">The YAML content to write</param>
    /// <returns>Tuple of (success, errorMessage). errorMessage is null if successful.</returns>
    public static (bool Success, string? ErrorMessage) WriteYamlFile(string filePath, string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return (false, "File path cannot be null or empty.");

        if (yamlContent == null)
            return (false, "YAML content cannot be null.");

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    return (false, $"Failed to create directory '{directory}': {ex.Message}");
                }
            }

            // Write file with UTF-8 encoding (BOM excluded for YAML compatibility)
            File.WriteAllText(filePath, yamlContent, new System.Text.UTF8Encoding(false));
            return (true, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (false, $"Access denied writing to '{filePath}': {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            return (false, $"I/O error writing to '{filePath}': {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            return (false, $"File path format is not supported: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error writing to '{filePath}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Safely reads YAML content from a file with error handling.
    /// </summary>
    /// <param name="filePath">The source file path</param>
    /// <returns>Tuple of (content, errorMessage). content is null if read failed.</returns>
    public static (string? Content, string? ErrorMessage) ReadYamlFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return (null, "File path cannot be null or empty.");

        try
        {
            if (!File.Exists(filePath))
                return (null, $"File not found: '{filePath}'");

            var content = File.ReadAllText(filePath);
            return (content, null);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (null, $"Access denied reading '{filePath}': {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            return (null, $"I/O error reading '{filePath}': {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            return (null, $"File path format is not supported: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Unexpected error reading '{filePath}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a backup of the target file before overwriting.
    /// </summary>
    /// <param name="filePath">The file to backup</param>
    /// <returns>Tuple of (backupPath, errorMessage). backupPath is null if backup failed.</returns>
    public static (string? BackupPath, string? ErrorMessage) CreateBackup(string filePath)
    {
        if (!File.Exists(filePath))
            return (null, $"Source file not found: '{filePath}'");

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = Path.GetExtension(filePath);
            var directoryName = Path.GetDirectoryName(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var backupPath = Path.Combine(
                directoryName ?? ".",
                $"{fileNameWithoutExtension}.{timestamp}.backup{extension}"
            );

            File.Copy(filePath, backupPath, overwrite: false);
            return (backupPath, null);
        }
        catch (Exception ex)
        {
            return (null, $"Failed to create backup: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that a file path is safe and properly formatted.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return (false, "File path cannot be null or empty.");

        try
        {
            _ = Path.GetFullPath(filePath);
            
            var invalidChars = Path.GetInvalidPathChars();
            if (filePath.Any(c => invalidChars.Contains(c)))
                return (false, "File path contains invalid characters.");

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Invalid file path: {ex.Message}");
        }
    }
}
