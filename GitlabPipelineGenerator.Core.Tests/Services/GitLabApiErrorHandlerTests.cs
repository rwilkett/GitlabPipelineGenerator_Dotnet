using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Moq;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for GitLabApiErrorHandler
/// </summary>
public class GitLabApiErrorHandlerTests
{
    private readonly GitLabApiSettings _settings;
    private readonly GitLabApiErrorHandler _errorHandler;

    public GitLabApiErrorHandlerTests()
    {
        _settings = new GitLabApiSettings
        {
            InstanceUrl = "https://gitlab.example.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3
        };
        _errorHandler = new GitLabApiErrorHandler(_settings);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GitLabApiErrorHandler(null!));
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldCreateInstance()
    {
        // Act
        var handler = new GitLabApiErrorHandler(_settings);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region ExecuteWithRetryAsync Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_WithSuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
        var policy = RetryPolicy.Default;

        // Act
        var result = await _errorHandler.ExecuteWithRetryAsync(operation, policy);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNonRetryableException_ShouldThrowImmediately()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        var operation = new Func<Task<string>>(() => throw exception);
        var policy = RetryPolicy.Default;

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));
        
        thrownException.InnerException.Should().Be(exception);
        thrownException.Message.Should().Contain("Operation failed after 1 attempts");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithRetryableException_ShouldRetryAndFail()
    {
        // Arrange
        var exception = new GitLabApiException("Server error", statusCode: 500);
        var operation = new Func<Task<string>>(() => throw exception);
        var policy = new RetryPolicy { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));
        
        thrownException.Message.Should().Contain("Operation failed after 2 attempts");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithRetryableExceptionThenSuccess_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount == 1)
                throw new GitLabApiException("Server error", statusCode: 500);
            return Task.FromResult(expectedResult);
        });
        var policy = new RetryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act
        var result = await _errorHandler.ExecuteWithRetryAsync(operation, policy);

        // Assert
        result.Should().Be(expectedResult);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithRateLimitException_ShouldRetryWithDelay()
    {
        // Arrange
        var exception = new GitLabApiException("Rate limit exceeded", statusCode: 429);
        var operation = new Func<Task<string>>(() => throw exception);
        var policy = new RetryPolicy { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));
        stopwatch.Stop();

        // Should have some delay due to rate limiting
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var operation = new Func<Task<string>>(() =>
        {
            cts.Cancel();
            return Task.Delay(1000, cts.Token).ContinueWith(_ => "result", TaskContinuationOptions.OnlyOnRanToCompletion);
        });
        var policy = RetryPolicy.Default;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy, cts.Token));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithHttpRequestException_ShouldRetry()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");
        var operation = new Func<Task<string>>(() => throw exception);
        var policy = new RetryPolicy { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));
        
        thrownException.Message.Should().Contain("Operation failed after 2 attempts");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTaskCanceledException_ShouldRetry()
    {
        // Arrange
        var exception = new TaskCanceledException("Request timeout");
        var operation = new Func<Task<string>>(() => throw exception);
        var policy = new RetryPolicy { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));
        
        thrownException.Message.Should().Contain("Operation failed after 2 attempts");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithSocketException_ShouldRetry()
    {
        // Arrange
        var exception = new SocketException();
        var operation = new Func<Task<string>>(() => throw exception);
        var policy = new RetryPolicy { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));
        
        thrownException.Message.Should().Contain("Operation failed after 2 attempts");
    }

    #endregion

    #region HandleRateLimiting Tests

    [Fact]
    public void HandleRateLimiting_WithRemainingRequests_ShouldReturnZeroDelay()
    {
        // Arrange
        var rateLimitInfo = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 50,
            ResetTime = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
        };

        // Act
        var delay = _errorHandler.HandleRateLimiting(rateLimitInfo);

        // Assert
        delay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void HandleRateLimiting_WithNoRemainingRequests_ShouldReturnDelayUntilReset()
    {
        // Arrange
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(5);
        var rateLimitInfo = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTime = resetTime.ToUnixTimeSeconds()
        };

        // Act
        var delay = _errorHandler.HandleRateLimiting(rateLimitInfo);

        // Assert
        delay.Should().BeGreaterThan(TimeSpan.FromMinutes(4));
        delay.Should().BeLessThan(TimeSpan.FromMinutes(6));
    }

    [Fact]
    public void HandleRateLimiting_WithLongResetTime_ShouldCapDelayToMaximum()
    {
        // Arrange
        var resetTime = DateTimeOffset.UtcNow.AddHours(2);
        var rateLimitInfo = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTime = resetTime.ToUnixTimeSeconds()
        };

        // Act
        var delay = _errorHandler.HandleRateLimiting(rateLimitInfo);

        // Assert
        delay.Should().Be(TimeSpan.FromMinutes(15));
    }

    #endregion

    #region TranslateGitLabError Tests

    [Fact]
    public void TranslateGitLabError_WithStatus401_ShouldReturnAuthenticationMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Unauthorized", statusCode: 401);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Authentication failed");
        message.Should().Contain("personal access token");
    }

    [Fact]
    public void TranslateGitLabError_WithStatus403_ShouldReturnAccessDeniedMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Forbidden", statusCode: 403);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Access denied");
        message.Should().Contain("permission");
    }

    [Fact]
    public void TranslateGitLabError_WithStatus404_ShouldReturnNotFoundMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Not Found", statusCode: 404);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Resource not found");
        message.Should().Contain("doesn't exist");
    }

    [Fact]
    public void TranslateGitLabError_WithStatus422_ShouldReturnInvalidRequestMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Unprocessable Entity", statusCode: 422);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Invalid request");
        message.Should().Contain("input parameters");
    }

    [Fact]
    public void TranslateGitLabError_WithStatus429_ShouldReturnRateLimitMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Too Many Requests", statusCode: 429);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Rate limit exceeded");
        message.Should().Contain("wait a moment");
    }

    [Fact]
    public void TranslateGitLabError_WithStatus500_ShouldReturnServerErrorMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Internal Server Error", statusCode: 500);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("GitLab server error");
        message.Should().Contain("try again later");
    }

    [Theory]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void TranslateGitLabError_WithServiceUnavailableStatus_ShouldReturnUnavailableMessage(int statusCode)
    {
        // Arrange
        var exception = new GitLabApiException("Service Unavailable", statusCode: statusCode);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("temporarily unavailable");
        message.Should().Contain("few minutes");
    }

    [Fact]
    public void TranslateGitLabError_WithInvalidTokenErrorCode_ShouldReturnTokenMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Invalid token", errorCode: "invalid_token");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("token is invalid or has expired");
        message.Should().Contain("generate a new");
    }

    [Fact]
    public void TranslateGitLabError_WithInsufficientScopeErrorCode_ShouldReturnScopeMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Insufficient scope", errorCode: "insufficient_scope");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("doesn't have the required permissions");
        message.Should().Contain("api");
    }

    [Fact]
    public void TranslateGitLabError_WithProjectNotFoundErrorCode_ShouldReturnProjectMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Project not found", errorCode: "project_not_found");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("project was not found");
        message.Should().Contain("project ID or path");
    }

    [Fact]
    public void TranslateGitLabError_WithBranchNotFoundErrorCode_ShouldReturnBranchMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Branch not found", errorCode: "branch_not_found");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("branch was not found");
        message.Should().Contain("branch name");
    }

    [Fact]
    public void TranslateGitLabError_WithUnknownError_ShouldReturnGenericMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Unknown error", statusCode: 418);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("GitLab API error");
        message.Should().Contain("Unknown error");
    }

    #endregion

    #region ShouldFallbackToManualMode Tests

    [Theory]
    [InlineData(401, true)]
    [InlineData(403, true)]
    [InlineData(404, false)]
    [InlineData(429, false)]
    [InlineData(500, true)]
    [InlineData(502, true)]
    [InlineData(503, true)]
    [InlineData(422, false)]
    public void ShouldFallbackToManualMode_WithGitLabApiException_ShouldReturnExpectedResult(int statusCode, bool expectedResult)
    {
        // Arrange
        var exception = new GitLabApiException("Test error", statusCode: statusCode);

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithHttpRequestException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithTaskCanceledException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new TaskCanceledException("Timeout");

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithOperationCanceledException_ShouldReturnFalse()
    {
        // Arrange
        var exception = new OperationCanceledException("User cancelled");

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithUnknownException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new InvalidOperationException("Unknown error");

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ExtractRateLimitInfo Tests

    [Fact]
    public void ExtractRateLimitInfo_WithAllHeaders_ShouldParseCorrectly()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "RateLimit-Limit", new[] { "100" } },
            { "RateLimit-Remaining", new[] { "50" } },
            { "RateLimit-Reset", new[] { "1640995200" } }
        };

        // Act
        var rateLimitInfo = _errorHandler.ExtractRateLimitInfo(headers);

        // Assert
        rateLimitInfo.Limit.Should().Be(100);
        rateLimitInfo.Remaining.Should().Be(50);
        rateLimitInfo.ResetTime.Should().Be(1640995200);
    }

    [Fact]
    public void ExtractRateLimitInfo_WithMissingHeaders_ShouldUseDefaults()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>();

        // Act
        var rateLimitInfo = _errorHandler.ExtractRateLimitInfo(headers);

        // Assert
        rateLimitInfo.Limit.Should().Be(0);
        rateLimitInfo.Remaining.Should().Be(0);
        rateLimitInfo.ResetTime.Should().Be(0);
    }

    [Fact]
    public void ExtractRateLimitInfo_WithInvalidValues_ShouldUseDefaults()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "RateLimit-Limit", new[] { "invalid" } },
            { "RateLimit-Remaining", new[] { "also-invalid" } },
            { "RateLimit-Reset", new[] { "not-a-number" } }
        };

        // Act
        var rateLimitInfo = _errorHandler.ExtractRateLimitInfo(headers);

        // Assert
        rateLimitInfo.Limit.Should().Be(0);
        rateLimitInfo.Remaining.Should().Be(0);
        rateLimitInfo.ResetTime.Should().Be(0);
    }

    [Fact]
    public void ExtractRateLimitInfo_WithPartialHeaders_ShouldParseAvailable()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "RateLimit-Limit", new[] { "100" } },
            { "RateLimit-Remaining", new[] { "25" } }
        };

        // Act
        var rateLimitInfo = _errorHandler.ExtractRateLimitInfo(headers);

        // Assert
        rateLimitInfo.Limit.Should().Be(100);
        rateLimitInfo.Remaining.Should().Be(25);
        rateLimitInfo.ResetTime.Should().Be(0);
    }

    #endregion

    #region RetryPolicy Tests

    [Fact]
    public void RetryPolicy_Default_ShouldHaveExpectedValues()
    {
        // Act
        var policy = RetryPolicy.Default;

        // Assert
        policy.MaxAttempts.Should().Be(3);
        policy.BaseDelay.Should().Be(TimeSpan.FromSeconds(1));
        policy.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        policy.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void RetryPolicy_Aggressive_ShouldHaveExpectedValues()
    {
        // Act
        var policy = RetryPolicy.Aggressive;

        // Assert
        policy.MaxAttempts.Should().Be(5);
        policy.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(500));
        policy.MaxDelay.Should().Be(TimeSpan.FromMinutes(2));
        policy.BackoffMultiplier.Should().Be(1.5);
    }

    [Fact]
    public void RetryPolicy_Conservative_ShouldHaveExpectedValues()
    {
        // Act
        var policy = RetryPolicy.Conservative;

        // Assert
        policy.MaxAttempts.Should().Be(2);
        policy.BaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        policy.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
        policy.BackoffMultiplier.Should().Be(2.0);
    }

    #endregion

    #region RateLimitInfo Tests

    [Fact]
    public void RateLimitInfo_IsExceeded_WithRemainingRequests_ShouldReturnFalse()
    {
        // Arrange
        var rateLimitInfo = new RateLimitInfo { Remaining = 10 };

        // Act & Assert
        rateLimitInfo.IsExceeded.Should().BeFalse();
    }

    [Fact]
    public void RateLimitInfo_IsExceeded_WithNoRemainingRequests_ShouldReturnTrue()
    {
        // Arrange
        var rateLimitInfo = new RateLimitInfo { Remaining = 0 };

        // Act & Assert
        rateLimitInfo.IsExceeded.Should().BeTrue();
    }

    [Fact]
    public void RateLimitInfo_IsExceeded_WithNegativeRemaining_ShouldReturnTrue()
    {
        // Arrange
        var rateLimitInfo = new RateLimitInfo { Remaining = -1 };

        // Act & Assert
        rateLimitInfo.IsExceeded.Should().BeTrue();
    }

    #endregion

    #region Advanced Retry Logic Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_WithExponentialBackoff_ShouldIncreaseDelayBetweenRetries()
    {
        // Arrange
        var callTimes = new List<DateTime>();
        var exception = new GitLabApiException("Server error", statusCode: 500);
        var operation = new Func<Task<string>>(() =>
        {
            callTimes.Add(DateTime.UtcNow);
            throw exception;
        });
        var policy = new RetryPolicy 
        { 
            MaxAttempts = 3, 
            BaseDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 2.0
        };

        // Act & Assert
        await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));

        // Assert - Should have 3 attempts with increasing delays
        callTimes.Should().HaveCount(3);
        
        // First retry should have some delay
        var firstRetryDelay = callTimes[1] - callTimes[0];
        firstRetryDelay.Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
        
        // Second retry should have longer delay
        var secondRetryDelay = callTimes[2] - callTimes[1];
        secondRetryDelay.Should().BeGreaterThan(firstRetryDelay);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithJitterInDelay_ShouldVaryDelayTimes()
    {
        // Arrange
        var delays = new List<TimeSpan>();
        var callCount = 0;
        var exception = new GitLabApiException("Server error", statusCode: 500);
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount <= 2) // Fail first two attempts
                throw exception;
            return Task.FromResult("success");
        });
        var policy = new RetryPolicy 
        { 
            MaxAttempts = 3, 
            BaseDelay = TimeSpan.FromMilliseconds(100)
        };

        // Execute multiple times to test jitter
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                callCount = 0; // Reset for each execution
                var startTime = DateTime.UtcNow;
                try
                {
                    await _errorHandler.ExecuteWithRetryAsync(operation, policy);
                }
                catch
                {
                    // Expected for some runs
                }
                var totalTime = DateTime.UtcNow - startTime;
                lock (delays)
                {
                    delays.Add(totalTime);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Delays should vary due to jitter (not all exactly the same)
        delays.Should().HaveCount(5);
        var uniqueDelays = delays.Distinct().Count();
        uniqueDelays.Should().BeGreaterThan(1, "Jitter should cause variation in delays");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithMaxDelayLimit_ShouldCapDelayAtMaximum()
    {
        // Arrange
        var callTimes = new List<DateTime>();
        var exception = new GitLabApiException("Server error", statusCode: 500);
        var operation = new Func<Task<string>>(() =>
        {
            callTimes.Add(DateTime.UtcNow);
            throw exception;
        });
        var policy = new RetryPolicy 
        { 
            MaxAttempts = 4, 
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(200), // Cap at 200ms
            BackoffMultiplier = 4.0 // Would normally create very long delays
        };

        // Act & Assert
        await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));

        // Assert - No delay should exceed MaxDelay
        callTimes.Should().HaveCount(4);
        for (int i = 1; i < callTimes.Count; i++)
        {
            var delay = callTimes[i] - callTimes[i - 1];
            delay.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(250), // Allow some tolerance for jitter
                $"Delay between attempt {i - 1} and {i} should not exceed MaxDelay");
        }
    }

    [Theory]
    [InlineData(408, true)]  // Request Timeout
    [InlineData(429, true)]  // Too Many Requests
    [InlineData(500, true)]  // Internal Server Error
    [InlineData(502, true)]  // Bad Gateway
    [InlineData(503, true)]  // Service Unavailable
    [InlineData(504, true)]  // Gateway Timeout
    [InlineData(400, false)] // Bad Request
    [InlineData(401, false)] // Unauthorized
    [InlineData(403, false)] // Forbidden
    [InlineData(404, false)] // Not Found
    [InlineData(422, false)] // Unprocessable Entity
    public async Task ExecuteWithRetryAsync_WithVariousStatusCodes_ShouldRetryAppropriately(int statusCode, bool shouldRetry)
    {
        // Arrange
        var callCount = 0;
        var exception = new GitLabApiException($"Error {statusCode}", statusCode: statusCode);
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            throw exception;
        });
        var policy = new RetryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act & Assert
        await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));

        // Assert
        if (shouldRetry)
        {
            callCount.Should().Be(3, $"Status code {statusCode} should be retried");
        }
        else
        {
            callCount.Should().Be(1, $"Status code {statusCode} should not be retried");
        }
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithRateLimitSpecificDelay_ShouldUseRateLimitDelay()
    {
        // Arrange
        var callTimes = new List<DateTime>();
        var exception = new GitLabApiException("Rate limit exceeded", statusCode: 429);
        var operation = new Func<Task<string>>(() =>
        {
            callTimes.Add(DateTime.UtcNow);
            throw exception;
        });
        var policy = new RetryPolicy 
        { 
            MaxAttempts = 2, 
            BaseDelay = TimeSpan.FromMilliseconds(10) // Very short base delay
        };

        // Act & Assert
        await Assert.ThrowsAsync<GitLabApiException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy));

        // Assert - Rate limit should use exponential backoff starting from 2^1 = 2 seconds
        callTimes.Should().HaveCount(2);
        var delay = callTimes[1] - callTimes[0];
        delay.Should().BeGreaterThan(TimeSpan.FromSeconds(1.5), 
            "Rate limit retry should use exponential backoff, not base delay");
    }

    #endregion

    #region Rate Limiting Advanced Tests

    [Fact]
    public void HandleRateLimiting_WithNegativeResetTime_ShouldReturnZeroDelay()
    {
        // Arrange - Reset time in the past
        var pastResetTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var rateLimitInfo = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTime = pastResetTime.ToUnixTimeSeconds()
        };

        // Act
        var delay = _errorHandler.HandleRateLimiting(rateLimitInfo);

        // Assert
        delay.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(5), 
            "Past reset time should result in minimal delay");
    }

    [Fact]
    public void HandleRateLimiting_WithVeryLongResetTime_ShouldCapAt15Minutes()
    {
        // Arrange - Reset time very far in the future
        var futureResetTime = DateTimeOffset.UtcNow.AddHours(5);
        var rateLimitInfo = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTime = futureResetTime.ToUnixTimeSeconds()
        };

        // Act
        var delay = _errorHandler.HandleRateLimiting(rateLimitInfo);

        // Assert
        delay.Should().Be(TimeSpan.FromMinutes(15), 
            "Very long reset times should be capped at 15 minutes");
    }

    [Fact]
    public void HandleRateLimiting_WithExactResetTime_ShouldIncludeBuffer()
    {
        // Arrange - Reset time exactly now
        var resetTime = DateTimeOffset.UtcNow;
        var rateLimitInfo = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTime = resetTime.ToUnixTimeSeconds()
        };

        // Act
        var delay = _errorHandler.HandleRateLimiting(rateLimitInfo);

        // Assert
        delay.Should().BeGreaterThan(TimeSpan.FromSeconds(4), 
            "Should include 5-second buffer even when reset time is now");
        delay.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(10), 
            "Buffer should not be excessive");
    }

    [Theory]
    [InlineData(100, 50, false)]
    [InlineData(100, 1, false)]
    [InlineData(100, 0, true)]
    [InlineData(100, -1, true)]
    public void RateLimitInfo_IsExceeded_WithVariousRemainingValues_ShouldReturnCorrectResult(
        int limit, int remaining, bool expectedExceeded)
    {
        // Arrange
        var rateLimitInfo = new RateLimitInfo 
        { 
            Limit = limit, 
            Remaining = remaining 
        };

        // Act & Assert
        rateLimitInfo.IsExceeded.Should().Be(expectedExceeded);
    }

    [Fact]
    public void ExtractRateLimitInfo_WithMultipleHeaderValues_ShouldUseFirstValue()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "RateLimit-Limit", new[] { "100", "200" } },
            { "RateLimit-Remaining", new[] { "50", "75" } },
            { "RateLimit-Reset", new[] { "1640995200", "1640995300" } }
        };

        // Act
        var rateLimitInfo = _errorHandler.ExtractRateLimitInfo(headers);

        // Assert
        rateLimitInfo.Limit.Should().Be(100);
        rateLimitInfo.Remaining.Should().Be(50);
        rateLimitInfo.ResetTime.Should().Be(1640995200);
    }

    [Fact]
    public void ExtractRateLimitInfo_WithEmptyHeaderValues_ShouldUseDefaults()
    {
        // Arrange
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "RateLimit-Limit", new string[0] },
            { "RateLimit-Remaining", new string[0] },
            { "RateLimit-Reset", new string[0] }
        };

        // Act
        var rateLimitInfo = _errorHandler.ExtractRateLimitInfo(headers);

        // Assert
        rateLimitInfo.Limit.Should().Be(0);
        rateLimitInfo.Remaining.Should().Be(0);
        rateLimitInfo.ResetTime.Should().Be(0);
    }

    #endregion

    #region Cancellation and Timeout Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCancellationDuringRetryDelay_ShouldCancelImmediately()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callCount = 0;
        var exception = new GitLabApiException("Server error", statusCode: 500);
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                // Cancel during the retry delay
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    cts.Cancel();
                });
            }
            throw exception;
        });
        var policy = new RetryPolicy 
        { 
            MaxAttempts = 3, 
            BaseDelay = TimeSpan.FromSeconds(1) // Long delay to allow cancellation
        };

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy, cts.Token));
        stopwatch.Stop();

        // Should cancel quickly, not wait for full retry delay
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        callCount.Should().Be(1, "Should not retry after cancellation");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCancellationBeforeFirstAttempt_ShouldThrowImmediately()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before starting
        
        var operation = new Func<Task<string>>(() => Task.FromResult("success"));
        var policy = RetryPolicy.Default;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy, cts.Token));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithPreCancelledToken_ShouldThrowImmediately()
    {
        // Arrange
        var operation = new Func<Task<string>>(() => Task.FromResult("success"));
        var policy = RetryPolicy.Default;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _errorHandler.ExecuteWithRetryAsync(operation, policy, new CancellationToken(true)));
    }

    #endregion

    #region Error Message Translation Edge Cases

    [Fact]
    public void TranslateGitLabError_WithNullErrorCode_ShouldUseStatusCodeMapping()
    {
        // Arrange
        var exception = new GitLabApiException("Unauthorized", statusCode: 401, errorCode: null);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Authentication failed");
    }

    [Fact]
    public void TranslateGitLabError_WithEmptyErrorCode_ShouldUseStatusCodeMapping()
    {
        // Arrange
        var exception = new GitLabApiException("Forbidden", statusCode: 403, errorCode: "");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Access denied");
    }

    [Fact]
    public void TranslateGitLabError_WithBothErrorCodeAndStatusCode_ShouldPrioritizeErrorCode()
    {
        // Arrange
        var exception = new GitLabApiException("Some message", statusCode: 400, errorCode: "invalid_token");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("token is invalid or has expired");
        message.Should().NotContain("Invalid request"); // Should not use status code message
    }

    [Fact]
    public void TranslateGitLabError_WithUnknownErrorCodeAndKnownStatusCode_ShouldUseStatusCode()
    {
        // Arrange
        var exception = new GitLabApiException("Some message", statusCode: 404, errorCode: "unknown_error_code");

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("Resource not found");
    }

    [Fact]
    public void TranslateGitLabError_WithZeroStatusCode_ShouldUseGenericMessage()
    {
        // Arrange
        var exception = new GitLabApiException("Network error", statusCode: 0);

        // Act
        var message = _errorHandler.TranslateGitLabError(exception);

        // Assert
        message.Should().Contain("GitLab API error");
        message.Should().Contain("Network error");
    }

    #endregion

    #region Fallback Decision Edge Cases

    [Fact]
    public void ShouldFallbackToManualMode_WithSocketException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new System.Net.Sockets.SocketException();

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithTimeoutException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithAggregateException_ShouldReturnTrue()
    {
        // Arrange
        var innerException = new HttpRequestException("Network error");
        var exception = new AggregateException(innerException);

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldFallbackToManualMode_WithArgumentException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = _errorHandler.ShouldFallbackToManualMode(exception);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
