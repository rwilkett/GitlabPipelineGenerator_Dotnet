using GitlabPipelineGenerator.Core.Exceptions;
using System.Text;

namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Service for providing user-friendly error messages and troubleshooting guidance
/// </summary>
public class UserFriendlyErrorService
{
    private readonly bool _isVerbose;

    public UserFriendlyErrorService(bool isVerbose = false)
    {
        _isVerbose = isVerbose;
    }

    /// <summary>
    /// Displays a user-friendly error message with troubleshooting guidance
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <param name="context">Additional context about the operation</param>
    public void DisplayError(Exception error, string? context = null)
    {
        Console.WriteLine();
        Console.WriteLine("❌ An error occurred");
        
        if (!string.IsNullOrEmpty(context))
        {
            Console.WriteLine($"   Context: {context}");
        }

        var (friendlyMessage, suggestions) = GetFriendlyErrorInfo(error);
        
        Console.WriteLine($"   Error: {friendlyMessage}");
        Console.WriteLine();

        if (suggestions.Any())
        {
            Console.WriteLine("💡 Troubleshooting suggestions:");
            foreach (var suggestion in suggestions)
            {
                Console.WriteLine($"   • {suggestion}");
            }
            Console.WriteLine();
        }

        // Show technical details in verbose mode
        if (_isVerbose)
        {
            Console.WriteLine("🔍 Technical details:");
            Console.WriteLine($"   Exception Type: {error.GetType().Name}");
            Console.WriteLine($"   Message: {error.Message}");
            
            if (error.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {error.InnerException.Message}");
            }
            
            Console.WriteLine();
        }

        // Show common solutions
        var commonSolutions = GetCommonSolutions(error);
        if (commonSolutions.Any())
        {
            Console.WriteLine("🛠️  Common solutions:");
            foreach (var solution in commonSolutions)
            {
                Console.WriteLine($"   • {solution}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Gets friendly error information and suggestions
    /// </summary>
    /// <param name="error">The error to analyze</param>
    /// <returns>A tuple containing the friendly message and suggestions</returns>
    private (string friendlyMessage, List<string> suggestions) GetFriendlyErrorInfo(Exception error)
    {
        var suggestions = new List<string>();
        string friendlyMessage;

        switch (error)
        {
            case GitLabApiException gitlabError:
                (friendlyMessage, suggestions) = HandleGitLabApiError(gitlabError);
                break;

            case InsufficientPermissionsException permError:
                friendlyMessage = "You don't have sufficient permissions to perform this operation";
                suggestions.AddRange(new[]
                {
                    "Check that your GitLab token has the required permissions",
                    "Ensure you have at least 'Developer' access to the project",
                    "Contact the project owner to request additional permissions",
                    $"Required permissions: {permError.RequiredPermissions}"
                });
                break;

            case UnauthorizedAccessException:
                friendlyMessage = "Authentication failed or access was denied";
                suggestions.AddRange(new[]
                {
                    "Verify your GitLab personal access token is correct",
                    "Check that the token hasn't expired",
                    "Ensure the token has the required scopes (api, read_repository)",
                    "Try regenerating your personal access token"
                });
                break;

            case HttpRequestException httpError:
                friendlyMessage = "Network connection error occurred";
                suggestions.AddRange(new[]
                {
                    "Check your internet connection",
                    "Verify the GitLab instance URL is correct",
                    "Check if GitLab is experiencing downtime",
                    "Try again in a few minutes"
                });
                break;

            case TaskCanceledException:
                friendlyMessage = "The operation timed out";
                suggestions.AddRange(new[]
                {
                    "The GitLab server may be slow or overloaded",
                    "Try again with a smaller project or reduced analysis scope",
                    "Check your network connection stability",
                    "Consider using --analysis-depth 1 for faster analysis"
                });
                break;

            case ArgumentException argError when argError.Message.Contains("project"):
                friendlyMessage = "Invalid project specification";
                suggestions.AddRange(new[]
                {
                    "Use the project ID (e.g., --gitlab-project 12345)",
                    "Use the full project path (e.g., --gitlab-project group/project-name)",
                    "Use --list-projects to see available projects",
                    "Use --search-projects <term> to find projects by name"
                });
                break;

            case FileNotFoundException fileError:
                friendlyMessage = $"Required file not found: {Path.GetFileName(fileError.FileName)}";
                suggestions.AddRange(new[]
                {
                    "Ensure you're running the command from the correct directory",
                    "Check that all required configuration files exist",
                    "Try using absolute paths instead of relative paths"
                });
                break;

            case DirectoryNotFoundException:
                friendlyMessage = "Required directory not found";
                suggestions.AddRange(new[]
                {
                    "Ensure the output directory exists",
                    "Create the directory manually or use an existing path",
                    "Check file path permissions"
                });
                break;

            case JsonException:
                friendlyMessage = "Configuration file format error";
                suggestions.AddRange(new[]
                {
                    "Check the JSON syntax in your configuration files",
                    "Validate JSON format using an online validator",
                    "Ensure all quotes and brackets are properly matched"
                });
                break;

            default:
                friendlyMessage = error.Message;
                suggestions.Add("If this error persists, please check the documentation or report an issue");
                break;
        }

        return (friendlyMessage, suggestions);
    }

    /// <summary>
    /// Handles GitLab API specific errors
    /// </summary>
    /// <param name="error">The GitLab API error</param>
    /// <returns>Friendly message and suggestions</returns>
    private (string friendlyMessage, List<string> suggestions) HandleGitLabApiError(GitLabApiException error)
    {
        var suggestions = new List<string>();
        string friendlyMessage;

        switch (error.StatusCode)
        {
            case 401:
                friendlyMessage = "Authentication failed - invalid or expired token";
                suggestions.AddRange(new[]
                {
                    "Check that your personal access token is correct",
                    "Verify the token hasn't expired in GitLab settings",
                    "Ensure the token has 'api' and 'read_repository' scopes",
                    "Try creating a new personal access token"
                });
                break;

            case 403:
                friendlyMessage = "Access forbidden - insufficient permissions";
                suggestions.AddRange(new[]
                {
                    "You need at least 'Developer' access to analyze projects",
                    "Contact the project owner to request access",
                    "Check if the project is private and you have access",
                    "Verify your token has the required permissions"
                });
                break;

            case 404:
                friendlyMessage = "Project not found or not accessible";
                suggestions.AddRange(new[]
                {
                    "Check that the project ID or path is correct",
                    "Verify the project exists and you have access to it",
                    "Use --list-projects to see available projects",
                    "Ensure you're connected to the correct GitLab instance"
                });
                break;

            case 429:
                friendlyMessage = "Rate limit exceeded - too many requests";
                suggestions.AddRange(new[]
                {
                    "Wait a few minutes before trying again",
                    "Reduce the analysis scope with --analysis-depth 1",
                    "Use --skip-analysis to skip certain analysis types",
                    "Consider using a different GitLab token if available"
                });
                break;

            case 500:
            case 502:
            case 503:
            case 504:
                friendlyMessage = "GitLab server error - the service is temporarily unavailable";
                suggestions.AddRange(new[]
                {
                    "Try again in a few minutes",
                    "Check GitLab status page for known issues",
                    "Use --dry-run to test without making API calls",
                    "Consider using manual mode if the issue persists"
                });
                break;

            default:
                friendlyMessage = $"GitLab API error (HTTP {error.StatusCode}): {error.Message}";
                suggestions.AddRange(new[]
                {
                    "Check GitLab documentation for this error code",
                    "Verify your GitLab instance is accessible",
                    "Try again later if this is a temporary issue"
                });
                break;
        }

        return (friendlyMessage, suggestions);
    }

    /// <summary>
    /// Gets common solutions based on error type
    /// </summary>
    /// <param name="error">The error to analyze</param>
    /// <returns>List of common solutions</returns>
    private List<string> GetCommonSolutions(Exception error)
    {
        var solutions = new List<string>();

        // Add general solutions based on error patterns
        if (error.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            solutions.AddRange(new[]
            {
                "Check your internet connection",
                "Verify firewall settings allow GitLab access",
                "Try using a VPN if behind corporate firewall"
            });
        }

        if (error.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            solutions.AddRange(new[]
            {
                "Reduce analysis scope with --analysis-depth 1",
                "Use --skip-analysis dependencies,deployment for faster analysis",
                "Try analyzing a smaller project first"
            });
        }

        if (error.Message.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("auth", StringComparison.OrdinalIgnoreCase))
        {
            solutions.AddRange(new[]
            {
                "Generate a new personal access token in GitLab",
                "Ensure token has 'api' and 'read_repository' scopes",
                "Store token securely using environment variables"
            });
        }

        return solutions;
    }

    /// <summary>
    /// Displays a warning message with optional suggestions
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="suggestions">Optional suggestions</param>
    public void DisplayWarning(string message, params string[] suggestions)
    {
        Console.WriteLine($"⚠️  Warning: {message}");
        
        if (suggestions.Any())
        {
            foreach (var suggestion in suggestions)
            {
                Console.WriteLine($"   💡 {suggestion}");
            }
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Displays an informational message
    /// </summary>
    /// <param name="message">The information message</param>
    public void DisplayInfo(string message)
    {
        Console.WriteLine($"ℹ️  {message}");
    }

    /// <summary>
    /// Displays a success message
    /// </summary>
    /// <param name="message">The success message</param>
    public void DisplaySuccess(string message)
    {
        Console.WriteLine($"✅ {message}");
    }

    /// <summary>
    /// Creates a formatted error report for logging or debugging
    /// </summary>
    /// <param name="error">The error to report</param>
    /// <param name="context">Additional context</param>
    /// <returns>Formatted error report</returns>
    public string CreateErrorReport(Exception error, string? context = null)
    {
        var report = new StringBuilder();
        
        report.AppendLine("=== Error Report ===");
        report.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        if (!string.IsNullOrEmpty(context))
        {
            report.AppendLine($"Context: {context}");
        }
        
        report.AppendLine($"Exception Type: {error.GetType().FullName}");
        report.AppendLine($"Message: {error.Message}");
        
        if (error.InnerException != null)
        {
            report.AppendLine($"Inner Exception: {error.InnerException.GetType().FullName}");
            report.AppendLine($"Inner Message: {error.InnerException.Message}");
        }
        
        report.AppendLine("Stack Trace:");
        report.AppendLine(error.StackTrace);
        
        var (friendlyMessage, suggestions) = GetFriendlyErrorInfo(error);
        report.AppendLine($"Friendly Message: {friendlyMessage}");
        
        if (suggestions.Any())
        {
            report.AppendLine("Suggestions:");
            foreach (var suggestion in suggestions)
            {
                report.AppendLine($"  - {suggestion}");
            }
        }
        
        report.AppendLine("=== End Report ===");
        
        return report.ToString();
    }
}