using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Services;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for handling GitLab API errors with retry policies and rate limiting
/// </summary>
public interface IGitLabApiErrorHandler
{
    /// <summary>
    /// Executes an operation with retry logic and error handling
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="policy">Retry policy to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation, 
        RetryPolicy policy, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles rate limiting by calculating appropriate delay
    /// </summary>
    /// <param name="rateLimitInfo">Rate limit information from GitLab API</param>
    /// <returns>Delay to wait before next request</returns>
    TimeSpan HandleRateLimiting(RateLimitInfo rateLimitInfo);

    /// <summary>
    /// Translates GitLab API errors to user-friendly messages
    /// </summary>
    /// <param name="exception">GitLab API exception</param>
    /// <returns>User-friendly error message</returns>
    string TranslateGitLabError(GitLabApiException exception);

    /// <summary>
    /// Determines if the operation should fall back to manual mode
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <returns>True if should fallback to manual mode</returns>
    bool ShouldFallbackToManualMode(Exception exception);

    /// <summary>
    /// Extracts rate limit information from GitLab API response headers
    /// </summary>
    /// <param name="headers">Response headers</param>
    /// <returns>Rate limit information</returns>
    RateLimitInfo ExtractRateLimitInfo(IDictionary<string, IEnumerable<string>> headers);
}