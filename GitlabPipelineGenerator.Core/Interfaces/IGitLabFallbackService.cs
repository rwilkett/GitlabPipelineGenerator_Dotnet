using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for handling GitLab API fallback scenarios and degraded operation modes
/// </summary>
public interface IGitLabFallbackService
{
    /// <summary>
    /// Attempts to execute a GitLab operation with automatic fallback to manual mode
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">GitLab API operation to execute</param>
    /// <param name="fallbackOperation">Fallback operation to execute if API fails</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result from either the API operation or fallback</returns>
    Task<FallbackResult<T>> ExecuteWithFallbackAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<CancellationToken, Task<T>> fallbackOperation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a project analysis with fallback to cached or partial data
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="analysisOperation">Full analysis operation</param>
    /// <param name="partialAnalysisOperation">Partial analysis operation using cached data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with fallback information</returns>
    Task<FallbackAnalysisResult> ExecuteAnalysisWithFallbackAsync(
        string projectId,
        Func<CancellationToken, Task<ProjectAnalysisResult>> analysisOperation,
        Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>> partialAnalysisOperation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates user guidance for error scenarios
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="operationContext">Context of the operation that failed</param>
    /// <returns>User guidance information</returns>
    UserGuidance CreateUserGuidance(Exception exception, string operationContext);

    /// <summary>
    /// Clears cached analysis data
    /// </summary>
    /// <param name="projectId">Specific project ID to clear, or null to clear all</param>
    void ClearCache(string? projectId = null);

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    CacheStatistics GetCacheStatistics();
}