using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Logging;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for handling GitLab API fallback scenarios and degraded operation modes
/// </summary>
public class GitLabFallbackService : IGitLabFallbackService
{
    private readonly ILogger<GitLabFallbackService> _logger;
    private readonly IGitLabApiErrorHandler _errorHandler;
    private readonly Dictionary<string, CachedAnalysisResult> _analysisCache;
    private readonly object _cacheLock = new();

    public GitLabFallbackService(
        ILogger<GitLabFallbackService> logger,
        IGitLabApiErrorHandler errorHandler)
    {
        _logger = logger;
        _errorHandler = errorHandler;
        _analysisCache = new Dictionary<string, CachedAnalysisResult>();
    }

    /// <summary>
    /// Attempts to execute a GitLab operation with automatic fallback to manual mode
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">GitLab API operation to execute</param>
    /// <param name="fallbackOperation">Fallback operation to execute if API fails</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result from either the API operation or fallback</returns>
    public async Task<FallbackResult<T>> ExecuteWithFallbackAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<CancellationToken, Task<T>> fallbackOperation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Attempting GitLab API operation: {OperationName}", operationName);
            var result = await operation(cancellationToken);
            
            _logger.LogDebug("GitLab API operation succeeded: {OperationName}", operationName);
            return new FallbackResult<T>
            {
                Result = result,
                UsedFallback = false,
                OperationName = operationName
            };
        }
        catch (Exception ex)
        {
            var shouldFallback = _errorHandler.ShouldFallbackToManualMode(ex);
            
            if (shouldFallback)
            {
                _logger.LogWarning(ex, "GitLab API operation failed, falling back to manual mode: {OperationName}", operationName);
                
                try
                {
                    var fallbackResult = await fallbackOperation(cancellationToken);
                    
                    _logger.LogInformation("Fallback operation succeeded: {OperationName}", operationName);
                    return new FallbackResult<T>
                    {
                        Result = fallbackResult,
                        UsedFallback = true,
                        OperationName = operationName,
                        FallbackReason = _errorHandler.TranslateGitLabError(
                            ex is GitLabApiException apiEx ? apiEx : new GitLabApiException(ex.Message, ex))
                    };
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Both GitLab API and fallback operations failed: {OperationName}", operationName);
                    throw new GitLabApiException(
                        $"Both GitLab API and fallback operations failed for {operationName}. " +
                        $"API error: {ex.Message}. Fallback error: {fallbackEx.Message}", ex);
                }
            }
            else
            {
                _logger.LogError(ex, "GitLab API operation failed and fallback not appropriate: {OperationName}", operationName);
                throw;
            }
        }
    }

    /// <summary>
    /// Executes a project analysis with fallback to cached or partial data
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="analysisOperation">Full analysis operation</param>
    /// <param name="partialAnalysisOperation">Partial analysis operation using cached data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with fallback information</returns>
    public async Task<FallbackAnalysisResult> ExecuteAnalysisWithFallbackAsync(
        string projectId,
        Func<CancellationToken, Task<ProjectAnalysisResult>> analysisOperation,
        Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>> partialAnalysisOperation,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analysis_{projectId}";
        CachedAnalysisResult? cachedResult = null;

        // Try to get cached result
        lock (_cacheLock)
        {
            if (_analysisCache.TryGetValue(cacheKey, out cachedResult))
            {
                // Check if cache is still valid (e.g., less than 1 hour old)
                if (DateTime.UtcNow - cachedResult.CachedAt > TimeSpan.FromHours(1))
                {
                    _analysisCache.Remove(cacheKey);
                    cachedResult = null;
                }
            }
        }

        try
        {
            _logger.LogDebug("Attempting full project analysis for project: {ProjectId}", projectId);
            var result = await analysisOperation(cancellationToken);
            
            // Cache the successful result
            lock (_cacheLock)
            {
                _analysisCache[cacheKey] = new CachedAnalysisResult
                {
                    ProjectId = projectId,
                    Result = result,
                    CachedAt = DateTime.UtcNow
                };
            }

            _logger.LogDebug("Full project analysis succeeded for project: {ProjectId}", projectId);
            return new FallbackAnalysisResult
            {
                Result = result,
                UsedFallback = false,
                UsedCachedData = false,
                ProjectId = projectId
            };
        }
        catch (Exception ex)
        {
            var shouldFallback = _errorHandler.ShouldFallbackToManualMode(ex);
            
            if (shouldFallback)
            {
                _logger.LogWarning(ex, "Full analysis failed, attempting partial analysis for project: {ProjectId}", projectId);
                
                try
                {
                    var partialResult = await partialAnalysisOperation(cachedResult, cancellationToken);
                    
                    _logger.LogInformation("Partial analysis succeeded for project: {ProjectId}", projectId);
                    return new FallbackAnalysisResult
                    {
                        Result = partialResult,
                        UsedFallback = true,
                        UsedCachedData = cachedResult != null,
                        ProjectId = projectId,
                        FallbackReason = _errorHandler.TranslateGitLabError(
                            ex is GitLabApiException apiEx ? apiEx : new GitLabApiException(ex.Message, ex)),
                        Warnings = new List<string>
                        {
                            "Analysis completed with limited data due to GitLab API issues",
                            cachedResult != null ? $"Using cached data from {cachedResult.CachedAt:yyyy-MM-dd HH:mm:ss} UTC" : "No cached data available"
                        }
                    };
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Both full and partial analysis failed for project: {ProjectId}", projectId);
                    throw new GitLabApiException(
                        $"Project analysis failed for project {projectId}. " +
                        $"Full analysis error: {ex.Message}. Partial analysis error: {fallbackEx.Message}", ex);
                }
            }
            else
            {
                _logger.LogError(ex, "Project analysis failed and fallback not appropriate for project: {ProjectId}", projectId);
                throw;
            }
        }
    }

    /// <summary>
    /// Creates user guidance for error scenarios
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="operationContext">Context of the operation that failed</param>
    /// <returns>User guidance information</returns>
    public UserGuidance CreateUserGuidance(Exception exception, string operationContext)
    {
        var guidance = new UserGuidance
        {
            OperationContext = operationContext,
            ErrorMessage = _errorHandler.TranslateGitLabError(
                exception is GitLabApiException apiEx ? apiEx : new GitLabApiException(exception.Message, exception))
        };

        // Add specific guidance based on exception type
        switch (exception)
        {
            case GitLabApiException { StatusCode: 401 }:
                guidance.Suggestions.AddRange(new[]
                {
                    "Verify your GitLab personal access token is correct and not expired",
                    "Ensure your token has the required scopes (api or read_api)",
                    "Check if your GitLab instance URL is correct",
                    "Try regenerating your personal access token in GitLab settings"
                });
                guidance.CanContinueWithManualMode = true;
                break;

            case GitLabApiException { StatusCode: 403 }:
                guidance.Suggestions.AddRange(new[]
                {
                    "Contact the project owner to request appropriate permissions",
                    "Verify you have at least Reporter access to the project",
                    "Check if the project exists and is accessible to you",
                    "Ensure your account is not blocked or suspended"
                });
                guidance.CanContinueWithManualMode = true;
                break;

            case GitLabApiException { StatusCode: 404 }:
                guidance.Suggestions.AddRange(new[]
                {
                    "Verify the project ID or path is correct",
                    "Check if the project has been moved or deleted",
                    "Ensure you have access to the project",
                    "Try searching for the project using the list command"
                });
                guidance.CanContinueWithManualMode = true;
                break;

            case GitLabApiException { StatusCode: 429 }:
                guidance.Suggestions.AddRange(new[]
                {
                    "Wait a few minutes before retrying the operation",
                    "Consider using a different GitLab instance if available",
                    "Contact your GitLab administrator about rate limits",
                    "Try the operation during off-peak hours"
                });
                guidance.CanContinueWithManualMode = false;
                guidance.ShouldRetryLater = true;
                break;

            case GitLabApiException { StatusCode: >= 500 }:
                guidance.Suggestions.AddRange(new[]
                {
                    "GitLab server is experiencing issues, try again later",
                    "Check GitLab status page for known issues",
                    "Contact your GitLab administrator if the issue persists",
                    "Consider using manual configuration mode"
                });
                guidance.CanContinueWithManualMode = true;
                guidance.ShouldRetryLater = true;
                break;

            case HttpRequestException:
            case TaskCanceledException:
                guidance.Suggestions.AddRange(new[]
                {
                    "Check your internet connection",
                    "Verify the GitLab instance URL is accessible",
                    "Try increasing the timeout setting",
                    "Check if there are firewall or proxy issues"
                });
                guidance.CanContinueWithManualMode = true;
                break;

            default:
                guidance.Suggestions.AddRange(new[]
                {
                    "Check the error details for more specific information",
                    "Try the operation again in a few minutes",
                    "Consider using manual configuration mode",
                    "Contact support if the issue persists"
                });
                guidance.CanContinueWithManualMode = true;
                break;
        }

        return guidance;
    }

    /// <summary>
    /// Clears cached analysis data
    /// </summary>
    /// <param name="projectId">Specific project ID to clear, or null to clear all</param>
    public void ClearCache(string? projectId = null)
    {
        lock (_cacheLock)
        {
            if (projectId != null)
            {
                var cacheKey = $"analysis_{projectId}";
                _analysisCache.Remove(cacheKey);
                _logger.LogDebug("Cleared cache for project: {ProjectId}", projectId);
            }
            else
            {
                _analysisCache.Clear();
                _logger.LogDebug("Cleared all cached analysis data");
            }
        }
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        lock (_cacheLock)
        {
            return new CacheStatistics
            {
                TotalEntries = _analysisCache.Count,
                OldestEntry = _analysisCache.Values.Any() ? _analysisCache.Values.Min(c => c.CachedAt) : null,
                NewestEntry = _analysisCache.Values.Any() ? _analysisCache.Values.Max(c => c.CachedAt) : null,
                ProjectIds = _analysisCache.Values.Select(c => c.ProjectId).ToList()
            };
        }
    }
}

/// <summary>
/// Result of an operation with fallback information
/// </summary>
/// <typeparam name="T">Result type</typeparam>
public class FallbackResult<T>
{
    /// <summary>
    /// The operation result
    /// </summary>
    public T Result { get; set; } = default!;

    /// <summary>
    /// Whether fallback was used
    /// </summary>
    public bool UsedFallback { get; set; }

    /// <summary>
    /// Name of the operation
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Reason for fallback (if used)
    /// </summary>
    public string? FallbackReason { get; set; }
}

/// <summary>
/// Result of analysis operation with fallback information
/// </summary>
public class FallbackAnalysisResult
{
    /// <summary>
    /// The analysis result
    /// </summary>
    public ProjectAnalysisResult Result { get; set; } = default!;

    /// <summary>
    /// Whether fallback was used
    /// </summary>
    public bool UsedFallback { get; set; }

    /// <summary>
    /// Whether cached data was used
    /// </summary>
    public bool UsedCachedData { get; set; }

    /// <summary>
    /// Project ID that was analyzed
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Reason for fallback (if used)
    /// </summary>
    public string? FallbackReason { get; set; }

    /// <summary>
    /// Warnings about the analysis
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Cached analysis result
/// </summary>
public class CachedAnalysisResult
{
    /// <summary>
    /// Project ID
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Cached analysis result
    /// </summary>
    public ProjectAnalysisResult Result { get; set; } = default!;

    /// <summary>
    /// When the result was cached
    /// </summary>
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// User guidance for error scenarios
/// </summary>
public class UserGuidance
{
    /// <summary>
    /// Context of the operation that failed
    /// </summary>
    public string OperationContext { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Suggestions for resolving the issue
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Whether the user can continue with manual mode
    /// </summary>
    public bool CanContinueWithManualMode { get; set; }

    /// <summary>
    /// Whether the user should retry later
    /// </summary>
    public bool ShouldRetryLater { get; set; }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cached entries
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Oldest cached entry timestamp
    /// </summary>
    public DateTime? OldestEntry { get; set; }

    /// <summary>
    /// Newest cached entry timestamp
    /// </summary>
    public DateTime? NewestEntry { get; set; }

    /// <summary>
    /// List of cached project IDs
    /// </summary>
    public List<string> ProjectIds { get; set; } = new();
}