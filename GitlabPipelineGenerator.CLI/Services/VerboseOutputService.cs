using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Models;
using System.Text.Json;

namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Service for providing detailed verbose output during GitLab operations
/// </summary>
public class VerboseOutputService
{
    private readonly bool _isVerbose;
    private int _indentLevel = 0;

    public VerboseOutputService(bool isVerbose = false)
    {
        _isVerbose = isVerbose;
    }

    /// <summary>
    /// Writes a verbose message if verbose mode is enabled
    /// </summary>
    /// <param name="message">The message to write</param>
    public void WriteLine(string message)
    {
        if (_isVerbose)
        {
            var indent = new string(' ', _indentLevel * 2);
            Console.WriteLine($"{indent}🔍 {message}");
        }
    }

    /// <summary>
    /// Writes a verbose message with timestamp
    /// </summary>
    /// <param name="message">The message to write</param>
    public void WriteLineWithTimestamp(string message)
    {
        if (_isVerbose)
        {
            var indent = new string(' ', _indentLevel * 2);
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{indent}🔍 [{timestamp}] {message}");
        }
    }

    /// <summary>
    /// Writes a section header
    /// </summary>
    /// <param name="title">The section title</param>
    public void WriteSection(string title)
    {
        if (_isVerbose)
        {
            var indent = new string(' ', _indentLevel * 2);
            Console.WriteLine($"{indent}📋 {title}");
            Console.WriteLine($"{indent}{new string('-', title.Length + 4)}");
        }
    }

    /// <summary>
    /// Increases the indentation level for nested output
    /// </summary>
    public void IncreaseIndent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the indentation level
    /// </summary>
    public void DecreaseIndent()
    {
        if (_indentLevel > 0)
            _indentLevel--;
    }

    /// <summary>
    /// Displays authentication details
    /// </summary>
    /// <param name="connectionOptions">The connection options</param>
    /// <param name="userInfo">The authenticated user info</param>
    public void DisplayAuthenticationDetails(GitLabConnectionOptions connectionOptions, GitLabUserInfo userInfo)
    {
        if (!_isVerbose) return;

        WriteSection("Authentication Details");
        IncreaseIndent();
        WriteLine($"GitLab URL: {connectionOptions.InstanceUrl}");
        WriteLine($"User: {userInfo.Username} ({userInfo.Name})");
        WriteLine($"User ID: {userInfo.Id}");
        WriteLine($"Email: {userInfo.Email ?? "Not provided"}");
        WriteLine($"Profile: {connectionOptions.ProfileName ?? "Default"}");
        DecreaseIndent();
        Console.WriteLine();
    }

    /// <summary>
    /// Displays project details
    /// </summary>
    /// <param name="project">The GitLab project</param>
    /// <param name="permissions">The project permissions</param>
    public void DisplayProjectDetails(GitLabProject project, ProjectPermissions? permissions = null)
    {
        if (!_isVerbose) return;

        WriteSection("Project Details");
        IncreaseIndent();
        WriteLine($"Name: {project.Name}");
        WriteLine($"ID: {project.Id}");
        WriteLine($"Path: {project.FullPath}");
        WriteLine($"Description: {project.Description ?? "No description"}");
        WriteLine($"Default Branch: {project.DefaultBranch}");
        WriteLine($"Visibility: {project.Visibility}");
        WriteLine($"Last Activity: {project.LastActivityAt:yyyy-MM-dd HH:mm:ss}");
        WriteLine($"Web URL: {project.WebUrl}");

        if (permissions != null)
        {
            WriteLine($"Access Level: {permissions.AccessLevel}");
            WriteLine($"Can Read Repository: {permissions.CanReadRepository}");
            WriteLine($"Can Write Repository: {permissions.CanWriteRepository}");
            WriteLine($"Can Admin Project: {permissions.CanAdminProject}");
        }
        DecreaseIndent();
        Console.WriteLine();
    }

    /// <summary>
    /// Displays analysis options
    /// </summary>
    /// <param name="options">The analysis options</param>
    public void DisplayAnalysisOptions(AnalysisOptions options)
    {
        if (!_isVerbose) return;

        WriteSection("Analysis Configuration");
        IncreaseIndent();
        WriteLine($"Analyze Files: {options.AnalyzeFiles}");
        WriteLine($"Analyze Dependencies: {options.AnalyzeDependencies}");
        WriteLine($"Analyze Existing CI: {options.AnalyzeExistingCI}");
        WriteLine($"Analyze Deployment: {options.AnalyzeDeployment}");
        WriteLine($"Max File Analysis Depth: {options.MaxFileAnalysisDepth}");
        WriteLine($"Include Security Analysis: {options.IncludeSecurityAnalysis}");
        
        if (options.ExcludePatterns.Any())
        {
            WriteLine($"Exclude Patterns: {string.Join(", ", options.ExcludePatterns)}");
        }
        DecreaseIndent();
        Console.WriteLine();
    }

    /// <summary>
    /// Displays detailed analysis results
    /// </summary>
    /// <param name="result">The analysis result</param>
    public void DisplayAnalysisResults(ProjectAnalysisResult result)
    {
        if (!_isVerbose) return;

        WriteSection("Analysis Results");
        IncreaseIndent();

        // Project Type and Framework
        WriteLine($"Detected Project Type: {result.DetectedType}");
        WriteLine($"Confidence Level: {result.Confidence}");
        WriteLine($"Framework: {result.Framework.Name}");
        if (!string.IsNullOrEmpty(result.Framework.Version))
        {
            WriteLine($"Framework Version: {result.Framework.Version}");
        }

        if (result.Framework.DetectedFeatures.Any())
        {
            WriteLine($"Detected Features: {string.Join(", ", result.Framework.DetectedFeatures)}");
        }

        // Build Configuration
        if (result.BuildConfig != null)
        {
            WriteLine($"Build Tool: {result.BuildConfig.BuildTool}");
            if (result.BuildConfig.BuildCommands.Any())
            {
                WriteLine($"Build Commands: {string.Join("; ", result.BuildConfig.BuildCommands)}");
            }
            if (result.BuildConfig.TestCommands.Any())
            {
                WriteLine($"Test Commands: {string.Join("; ", result.BuildConfig.TestCommands)}");
            }
            if (result.BuildConfig.ArtifactPaths.Any())
            {
                WriteLine($"Artifact Paths: {string.Join(", ", result.BuildConfig.ArtifactPaths)}");
            }
        }

        // Dependencies
        if (result.Dependencies != null)
        {
            WriteLine($"Dependencies Found: {result.Dependencies.Dependencies.Count}");
            WriteLine($"Dev Dependencies: {result.Dependencies.DevDependencies.Count}");
            WriteLine($"Runtime: {result.Dependencies.Runtime.Name} {result.Dependencies.Runtime.Version}");
            
            if (result.Dependencies.CacheRecommendation != null)
            {
                WriteLine($"Cache Strategy: {result.Dependencies.CacheRecommendation.Strategy}");
                if (result.Dependencies.CacheRecommendation.Paths.Any())
                {
                    WriteLine($"Cache Paths: {string.Join(", ", result.Dependencies.CacheRecommendation.Paths)}");
                }
            }
        }

        // Existing CI Configuration
        if (result.ExistingCI != null)
        {
            WriteLine($"Existing CI System: {result.ExistingCI.SystemType}");
            WriteLine($"CI Configuration File: {result.ExistingCI.ConfigurationFile}");
            if (result.ExistingCI.DetectedStages.Any())
            {
                WriteLine($"Existing Stages: {string.Join(", ", result.ExistingCI.DetectedStages)}");
            }
        }

        // Deployment Information
        if (result.Deployment != null)
        {
            WriteLine($"Deployment Strategy: {result.Deployment.Strategy}");
            if (result.Deployment.Targets.Any())
            {
                WriteLine($"Deployment Targets: {string.Join(", ", result.Deployment.Targets)}");
            }
            if (result.Deployment.Environments.Any())
            {
                WriteLine($"Environments: {string.Join(", ", result.Deployment.Environments.Select(e => e.Name))}");
            }
        }

        // Warnings
        if (result.Warnings.Any())
        {
            WriteLine($"Analysis Warnings ({result.Warnings.Count}):");
            IncreaseIndent();
            foreach (var warning in result.Warnings)
            {
                WriteLine($"⚠️  {warning.Message}");
                if (!string.IsNullOrEmpty(warning.Suggestion))
                {
                    WriteLine($"   💡 {warning.Suggestion}");
                }
            }
            DecreaseIndent();
        }

        DecreaseIndent();
        Console.WriteLine();
    }

    /// <summary>
    /// Displays pipeline generation details
    /// </summary>
    /// <param name="pipeline">The generated pipeline</param>
    /// <param name="options">The pipeline options used</param>
    public void DisplayPipelineGenerationDetails(PipelineConfiguration pipeline, PipelineOptions options)
    {
        if (!_isVerbose) return;

        WriteSection("Pipeline Generation Details");
        IncreaseIndent();

        WriteLine($"Project Type: {options.ProjectType}");
        WriteLine($"Stages: {string.Join(", ", pipeline.Stages)}");
        WriteLine($"Total Jobs: {pipeline.Jobs.Count}");

        // Job details by stage
        foreach (var stage in pipeline.Stages)
        {
            var stageJobs = pipeline.Jobs.Where(j => j.Stage == stage).ToList();
            if (stageJobs.Any())
            {
                WriteLine($"Stage '{stage}': {stageJobs.Count} jobs");
                IncreaseIndent();
                foreach (var job in stageJobs)
                {
                    WriteLine($"- {job.Name}");
                    if (job.Script.Any())
                    {
                        WriteLine($"  Commands: {job.Script.Count}");
                    }
                    if (job.Artifacts?.Paths?.Any() == true)
                    {
                        WriteLine($"  Artifacts: {string.Join(", ", job.Artifacts.Paths)}");
                    }
                }
                DecreaseIndent();
            }
        }

        // Variables
        if (pipeline.Variables.Any())
        {
            WriteLine($"Variables: {pipeline.Variables.Count}");
            IncreaseIndent();
            foreach (var variable in pipeline.Variables)
            {
                WriteLine($"- {variable.Key}: {variable.Value}");
            }
            DecreaseIndent();
        }

        DecreaseIndent();
        Console.WriteLine();
    }

    /// <summary>
    /// Displays API call information
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="url">API URL</param>
    /// <param name="statusCode">Response status code</param>
    /// <param name="duration">Request duration</param>
    public void DisplayApiCall(string method, string url, int statusCode, TimeSpan duration)
    {
        if (!_isVerbose) return;

        var statusIcon = statusCode >= 200 && statusCode < 300 ? "✅" : "❌";
        WriteLine($"{statusIcon} {method} {url} → {statusCode} ({duration.TotalMilliseconds:F0}ms)");
    }

    /// <summary>
    /// Displays error details with troubleshooting information
    /// </summary>
    /// <param name="error">The error that occurred</param>
    /// <param name="context">Additional context about when the error occurred</param>
    public void DisplayErrorDetails(Exception error, string? context = null)
    {
        if (!_isVerbose) return;

        WriteSection("Error Details");
        IncreaseIndent();

        if (!string.IsNullOrEmpty(context))
        {
            WriteLine($"Context: {context}");
        }

        WriteLine($"Error Type: {error.GetType().Name}");
        WriteLine($"Message: {error.Message}");

        if (error.InnerException != null)
        {
            WriteLine($"Inner Exception: {error.InnerException.GetType().Name}");
            WriteLine($"Inner Message: {error.InnerException.Message}");
        }

        WriteLine($"Stack Trace:");
        IncreaseIndent();
        var stackLines = error.StackTrace?.Split('\n') ?? Array.Empty<string>();
        foreach (var line in stackLines.Take(5)) // Show first 5 lines
        {
            WriteLine(line.Trim());
        }
        if (stackLines.Length > 5)
        {
            WriteLine($"... and {stackLines.Length - 5} more lines");
        }
        DecreaseIndent();

        DecreaseIndent();
        Console.WriteLine();
    }

    /// <summary>
    /// Displays performance metrics
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="duration">Duration of the operation</param>
    /// <param name="additionalMetrics">Additional metrics to display</param>
    public void DisplayPerformanceMetrics(string operationName, TimeSpan duration, Dictionary<string, object>? additionalMetrics = null)
    {
        if (!_isVerbose) return;

        WriteLine($"⏱️  {operationName} completed in {duration.TotalSeconds:F2}s");

        if (additionalMetrics != null && additionalMetrics.Any())
        {
            IncreaseIndent();
            foreach (var metric in additionalMetrics)
            {
                WriteLine($"{metric.Key}: {metric.Value}");
            }
            DecreaseIndent();
        }
    }

    /// <summary>
    /// Creates a scoped verbose output service with increased indentation
    /// </summary>
    /// <returns>A disposable scope that will decrease indentation when disposed</returns>
    public IDisposable CreateScope()
    {
        IncreaseIndent();
        return new VerboseScope(this);
    }

    private class VerboseScope : IDisposable
    {
        private readonly VerboseOutputService _service;

        public VerboseScope(VerboseOutputService service)
        {
            _service = service;
        }

        public void Dispose()
        {
            _service.DecreaseIndent();
        }
    }
}