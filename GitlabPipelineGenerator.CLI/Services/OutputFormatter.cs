using Microsoft.Extensions.Logging;
using System.Text;

namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Handles formatting and writing pipeline output to various destinations
/// </summary>
public class OutputFormatter
{
    private readonly ILogger<OutputFormatter> _logger;

    public OutputFormatter(ILogger<OutputFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Writes YAML content to a file
    /// </summary>
    /// <param name="yamlContent">YAML content to write</param>
    /// <param name="filePath">Path to the output file</param>
    /// <param name="verbose">Whether to show verbose output</param>
    /// <returns>Task representing the async operation</returns>
    public async Task WriteToFileAsync(string yamlContent, string filePath, bool verbose = false)
    {
        if (string.IsNullOrEmpty(yamlContent))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yamlContent));

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        try
        {
            _logger.LogDebug("Writing pipeline to file: {FilePath}", filePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            // Check if file already exists and warn user
            if (File.Exists(filePath))
            {
                if (verbose)
                {
                    Console.WriteLine($"⚠️  File '{filePath}' already exists and will be overwritten");
                }
                _logger.LogWarning("Overwriting existing file: {FilePath}", filePath);
            }

            // Write content to file
            await File.WriteAllTextAsync(filePath, yamlContent, Encoding.UTF8);

            // Show success message
            var fileInfo = new FileInfo(filePath);
            var sizeKb = Math.Round(fileInfo.Length / 1024.0, 2);
            
            Console.WriteLine($"✓ Pipeline written to '{filePath}' ({sizeKb} KB)");
            
            if (verbose)
            {
                Console.WriteLine($"  File size: {fileInfo.Length} bytes");
                Console.WriteLine($"  Full path: {fileInfo.FullName}");
                Console.WriteLine($"  Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
            }

            _logger.LogInformation("Successfully wrote pipeline to file: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
        }
        catch (UnauthorizedAccessException ex)
        {
            var message = $"Access denied when writing to '{filePath}'. Check file permissions.";
            _logger.LogError(ex, "Access denied writing to file: {FilePath}", filePath);
            Console.Error.WriteLine($"❌ {message}");
            throw new InvalidOperationException(message, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            var message = $"Directory not found for path '{filePath}'. Check that the directory exists.";
            _logger.LogError(ex, "Directory not found for file: {FilePath}", filePath);
            Console.Error.WriteLine($"❌ {message}");
            throw new InvalidOperationException(message, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error when writing to '{filePath}': {ex.Message}";
            _logger.LogError(ex, "I/O error writing to file: {FilePath}", filePath);
            Console.Error.WriteLine($"❌ {message}");
            throw new InvalidOperationException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error when writing to '{filePath}': {ex.Message}";
            _logger.LogError(ex, "Unexpected error writing to file: {FilePath}", filePath);
            Console.Error.WriteLine($"❌ {message}");
            throw;
        }
    }

    /// <summary>
    /// Writes YAML content to the console
    /// </summary>
    /// <param name="yamlContent">YAML content to write</param>
    /// <param name="verbose">Whether to show verbose output</param>
    /// <returns>Task representing the async operation</returns>
    public async Task WriteToConsoleAsync(string yamlContent, bool verbose = false)
    {
        if (string.IsNullOrEmpty(yamlContent))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yamlContent));

        try
        {
            _logger.LogDebug("Writing pipeline to console");

            if (verbose)
            {
                Console.WriteLine("# Generated GitLab CI/CD Pipeline Configuration");
                Console.WriteLine($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"# Content length: {yamlContent.Length} characters");
                Console.WriteLine("# " + new string('-', 50));
                Console.WriteLine();
            }

            // Write the YAML content
            await Console.Out.WriteAsync(yamlContent);

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("# " + new string('-', 50));
                Console.WriteLine("# End of pipeline configuration");
            }

            _logger.LogInformation("Successfully wrote pipeline to console ({Length} characters)", yamlContent.Length);
        }
        catch (Exception ex)
        {
            var message = $"Error writing to console: {ex.Message}";
            _logger.LogError(ex, "Error writing to console");
            Console.Error.WriteLine($"❌ {message}");
            throw new InvalidOperationException(message, ex);
        }
    }

    /// <summary>
    /// Validates YAML content and provides feedback
    /// </summary>
    /// <param name="yamlContent">YAML content to validate</param>
    /// <param name="verbose">Whether to show verbose validation output</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateYaml(string yamlContent, bool verbose = false)
    {
        if (string.IsNullOrEmpty(yamlContent))
        {
            Console.Error.WriteLine("❌ YAML content is empty");
            return false;
        }

        try
        {
            _logger.LogDebug("Validating YAML content");

            // Basic validation checks
            var lines = yamlContent.Split('\n');
            var issues = new List<string>();

            // Check for common YAML issues
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // Check for tabs (should use spaces)
                if (line.Contains('\t'))
                {
                    issues.Add($"Line {lineNumber}: Contains tabs (use spaces for indentation)");
                }

                // Check for trailing whitespace
                if (line.EndsWith(" ") || line.EndsWith("\t"))
                {
                    issues.Add($"Line {lineNumber}: Contains trailing whitespace");
                }

                // Check for very long lines
                if (line.Length > 120)
                {
                    issues.Add($"Line {lineNumber}: Line is very long ({line.Length} characters)");
                }
            }

            // Check for required GitLab CI/CD keywords
            var hasStages = yamlContent.Contains("stages:");
            var hasJobs = yamlContent.Contains("script:") || yamlContent.Contains("stage:");

            if (!hasStages && !hasJobs)
            {
                issues.Add("No stages or jobs found - this may not be a valid GitLab CI/CD pipeline");
            }

            // Report validation results
            if (issues.Any())
            {
                Console.WriteLine($"⚠️  YAML validation found {issues.Count} potential issues:");
                foreach (var issue in issues.Take(10)) // Limit to first 10 issues
                {
                    Console.WriteLine($"  - {issue}");
                }

                if (issues.Count > 10)
                {
                    Console.WriteLine($"  ... and {issues.Count - 10} more issues");
                }

                if (verbose)
                {
                    _logger.LogWarning("YAML validation found {IssueCount} issues", issues.Count);
                }

                return false;
            }
            else
            {
                if (verbose)
                {
                    Console.WriteLine("✓ YAML validation passed");
                }
                _logger.LogInformation("YAML validation passed successfully");
                return true;
            }
        }
        catch (Exception ex)
        {
            var message = $"Error during YAML validation: {ex.Message}";
            _logger.LogError(ex, "Error during YAML validation");
            Console.Error.WriteLine($"❌ {message}");
            return false;
        }
    }

    /// <summary>
    /// Shows pipeline statistics and summary
    /// </summary>
    /// <param name="yamlContent">YAML content to analyze</param>
    /// <param name="verbose">Whether to show detailed statistics</param>
    public void ShowPipelineStats(string yamlContent, bool verbose = false)
    {
        if (string.IsNullOrEmpty(yamlContent))
            return;

        try
        {
            _logger.LogDebug("Analyzing pipeline statistics");

            var lines = yamlContent.Split('\n');
            var stats = new Dictionary<string, int>();

            // Count various elements
            stats["Total lines"] = lines.Length;
            stats["Non-empty lines"] = lines.Count(line => !string.IsNullOrWhiteSpace(line));
            stats["Comment lines"] = lines.Count(line => line.TrimStart().StartsWith("#"));
            
            // Count GitLab CI/CD specific elements
            stats["Stages"] = CountOccurrences(yamlContent, "stage:");
            stats["Jobs with scripts"] = CountOccurrences(yamlContent, "script:");
            stats["Before scripts"] = CountOccurrences(yamlContent, "before_script:");
            stats["After scripts"] = CountOccurrences(yamlContent, "after_script:");
            stats["Variables"] = CountOccurrences(yamlContent, "variables:");
            stats["Artifacts"] = CountOccurrences(yamlContent, "artifacts:");
            stats["Cache entries"] = CountOccurrences(yamlContent, "cache:");
            stats["Rules"] = CountOccurrences(yamlContent, "rules:");

            Console.WriteLine();
            Console.WriteLine("📊 Pipeline Statistics:");
            
            foreach (var stat in stats.Where(s => s.Value > 0))
            {
                Console.WriteLine($"  {stat.Key}: {stat.Value}");
            }

            if (verbose)
            {
                // Additional detailed stats
                var fileSize = System.Text.Encoding.UTF8.GetByteCount(yamlContent);
                var avgLineLength = lines.Where(l => !string.IsNullOrWhiteSpace(l))
                                       .Select(l => l.Length)
                                       .DefaultIfEmpty(0)
                                       .Average();

                Console.WriteLine();
                Console.WriteLine("📋 Detailed Statistics:");
                Console.WriteLine($"  File size: {fileSize} bytes ({Math.Round(fileSize / 1024.0, 2)} KB)");
                Console.WriteLine($"  Average line length: {Math.Round(avgLineLength, 1)} characters");
                Console.WriteLine($"  Indentation style: {(yamlContent.Contains("\t") ? "Mixed/Tabs" : "Spaces")}");
            }

            _logger.LogInformation("Pipeline statistics calculated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pipeline statistics");
            Console.Error.WriteLine($"❌ Error calculating statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Counts occurrences of a pattern in the YAML content
    /// </summary>
    /// <param name="content">Content to search</param>
    /// <param name="pattern">Pattern to count</param>
    /// <returns>Number of occurrences</returns>
    private static int CountOccurrences(string content, string pattern)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(pattern))
            return 0;

        int count = 0;
        int index = 0;

        while ((index = content.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}