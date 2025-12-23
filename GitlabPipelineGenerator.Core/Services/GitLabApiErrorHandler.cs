using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using System.Net;
using System.Net.Sockets;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for handling GitLab API errors with retry policies and rate limiting
/// </summary>
public class GitLabApiErrorHandler : IGitLabApiErrorHandler
{
    private readonly GitLabApiSettings _settings;

    public GitLabApiErrorHandler(GitLabApiSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Executes an operation with retry logic and error handling
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="policy">Retry policy to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation, 
        RetryPolicy policy, 
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < policy.MaxAttempts)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt, policy))
            {
                lastException = ex;
                attempt++;

                if (attempt < policy.MaxAttempts)
                {
                    var delay = CalculateDelay(attempt, policy, ex);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // If we've exhausted all retries, throw the last exception with context
        throw new GitLabApiException(
            $"Operation failed after {policy.MaxAttempts} attempts. Last error: {lastException?.Message}",
            lastException ?? new Exception("Unknown error"));
    }

    /// <summary>
    /// Handles rate limiting by calculating appropriate delay
    /// </summary>
    /// <param name="rateLimitInfo">Rate limit information from GitLab API</param>
    /// <returns>Delay to wait before next request</returns>
    public TimeSpan HandleRateLimiting(RateLimitInfo rateLimitInfo)
    {
        if (rateLimitInfo.Remaining > 0)
        {
            return TimeSpan.Zero;
        }

        // Calculate delay until reset time, with a small buffer
        var resetTime = DateTimeOffset.FromUnixTimeSeconds(rateLimitInfo.ResetTime);
        var delay = resetTime - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

        // Cap the delay to a reasonable maximum
        return delay > TimeSpan.FromMinutes(15) ? TimeSpan.FromMinutes(15) : delay;
    }

    /// <summary>
    /// Translates GitLab API errors to user-friendly messages
    /// </summary>
    /// <param name="exception">GitLab API exception</param>
    /// <returns>User-friendly error message</returns>
    public string TranslateGitLabError(GitLabApiException exception)
    {
        return exception.StatusCode switch
        {
            401 => "Authentication failed. Please check your GitLab personal access token and ensure it has the required permissions.",
            403 => "Access denied. You don't have permission to access this resource. Please check your project permissions or contact the project owner.",
            404 => "Resource not found. The specified project, file, or endpoint doesn't exist or you don't have access to it.",
            422 => "Invalid request. Please check your input parameters and try again.",
            429 => "Rate limit exceeded. Please wait a moment before making more requests.",
            500 => "GitLab server error. Please try again later or contact your GitLab administrator.",
            502 or 503 or 504 => "GitLab service temporarily unavailable. Please try again in a few minutes.",
            _ when exception.ErrorCode == "invalid_token" => "Your GitLab token is invalid or has expired. Please generate a new personal access token.",
            _ when exception.ErrorCode == "insufficient_scope" => "Your GitLab token doesn't have the required permissions. Please ensure your token has 'api' or 'read_api' scope.",
            _ when exception.ErrorCode == "project_not_found" => "The specified project was not found. Please check the project ID or path and your access permissions.",
            _ when exception.ErrorCode == "branch_not_found" => "The specified branch was not found. Please check the branch name or use the default branch.",
            _ => $"GitLab API error: {exception.Message}. Please check your connection and try again."
        };
    }

    /// <summary>
    /// Determines if the operation should fall back to manual mode
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <returns>True if should fallback to manual mode</returns>
    public bool ShouldFallbackToManualMode(Exception exception)
    {
        return exception switch
        {
            GitLabApiException apiEx => apiEx.StatusCode switch
            {
                401 => true, // Authentication failure
                403 => true, // Access denied
                404 => false, // Not found - might be temporary
                429 => false, // Rate limit - can retry
                >= 500 => true, // Server errors
                _ => false
            },
            HttpRequestException => true, // Network connectivity issues
            TaskCanceledException => true, // Timeout
            OperationCanceledException => false, // User cancellation
            _ => true // Unknown errors
        };
    }

    /// <summary>
    /// Extracts rate limit information from GitLab API response headers
    /// </summary>
    /// <param name="headers">Response headers</param>
    /// <returns>Rate limit information</returns>
    public RateLimitInfo ExtractRateLimitInfo(IDictionary<string, IEnumerable<string>> headers)
    {
        var rateLimitInfo = new RateLimitInfo();

        if (headers.TryGetValue("RateLimit-Limit", out var limitValues))
        {
            if (int.TryParse(limitValues.FirstOrDefault(), out var limit))
            {
                rateLimitInfo.Limit = limit;
            }
        }

        if (headers.TryGetValue("RateLimit-Remaining", out var remainingValues))
        {
            if (int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
            {
                rateLimitInfo.Remaining = remaining;
            }
        }

        if (headers.TryGetValue("RateLimit-Reset", out var resetValues))
        {
            if (long.TryParse(resetValues.FirstOrDefault(), out var reset))
            {
                rateLimitInfo.ResetTime = reset;
            }
        }

        return rateLimitInfo;
    }

    private bool ShouldRetry(Exception exception, int attempt, RetryPolicy policy)
    {
        if (attempt >= policy.MaxAttempts - 1)
        {
            return false;
        }

        return exception switch
        {
            GitLabApiException apiEx => apiEx.StatusCode switch
            {
                429 => true, // Rate limit - always retry
                >= 500 => true, // Server errors
                408 => true, // Request timeout
                _ => false
            },
            HttpRequestException => true, // Network issues
            TaskCanceledException => true, // Timeout
            SocketException => true, // Network connectivity
            _ => false
        };
    }

    private TimeSpan CalculateDelay(int attempt, RetryPolicy policy, Exception exception)
    {
        // Handle rate limiting with specific delay
        if (exception is GitLabApiException { StatusCode: 429 } apiEx)
        {
            // Try to extract retry-after header or use exponential backoff
            return TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attempt)));
        }

        // Exponential backoff for other retryable errors
        var baseDelay = policy.BaseDelay;
        var exponentialDelay = TimeSpan.FromMilliseconds(
            baseDelay.TotalMilliseconds * Math.Pow(policy.BackoffMultiplier, attempt));

        // Add jitter to prevent thundering herd
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
        
        var totalDelay = exponentialDelay + jitter;
        
        // Cap at maximum delay
        return totalDelay > policy.MaxDelay ? policy.MaxDelay : totalDelay;
    }
}

/// <summary>
/// Retry policy configuration for GitLab API operations
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Default retry policy for GitLab API operations
    /// </summary>
    public static RetryPolicy Default => new()
    {
        MaxAttempts = 3,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        BackoffMultiplier = 2.0
    };

    /// <summary>
    /// Aggressive retry policy for critical operations
    /// </summary>
    public static RetryPolicy Aggressive => new()
    {
        MaxAttempts = 5,
        BaseDelay = TimeSpan.FromMilliseconds(500),
        MaxDelay = TimeSpan.FromMinutes(2),
        BackoffMultiplier = 1.5
    };

    /// <summary>
    /// Conservative retry policy for non-critical operations
    /// </summary>
    public static RetryPolicy Conservative => new()
    {
        MaxAttempts = 2,
        BaseDelay = TimeSpan.FromSeconds(2),
        MaxDelay = TimeSpan.FromSeconds(10),
        BackoffMultiplier = 2.0
    };
}

/// <summary>
/// Rate limit information from GitLab API
/// </summary>
public class RateLimitInfo
{
    /// <summary>
    /// Maximum number of requests allowed
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Number of requests remaining in current window
    /// </summary>
    public int Remaining { get; set; }

    /// <summary>
    /// Unix timestamp when the rate limit resets
    /// </summary>
    public long ResetTime { get; set; }

    /// <summary>
    /// Whether rate limit is currently exceeded
    /// </summary>
    public bool IsExceeded => Remaining <= 0;
}