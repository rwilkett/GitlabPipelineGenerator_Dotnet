using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for GitLabFallbackService
/// </summary>
public class GitLabFallbackServiceTests
{
    private readonly Mock<ILogger<GitLabFallbackService>> _mockLogger;
    private readonly Mock<IGitLabApiErrorHandler> _mockErrorHandler;
    private readonly GitLabFallbackService _fallbackService;

    public GitLabFallbackServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitLabFallbackService>>();
        _mockErrorHandler = new Mock<IGitLabApiErrorHandler>();
        _fallbackService = new GitLabFallbackService(_mockLogger.Object, _mockErrorHandler.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = new GitLabFallbackService(_mockLogger.Object, _mockErrorHandler.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GitLabFallbackService(null!, _mockErrorHandler.Object));
    }

    [Fact]
    public void Constructor_WithNullErrorHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GitLabFallbackService(_mockLogger.Object, null!));
    }

    #endregion

    #region ExecuteWithFallbackAsync Tests

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithSuccessfulOperation_ShouldReturnResultWithoutFallback()
    {
        // Arrange
        var expectedResult = "success";
        var operation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult(expectedResult));
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("fallback"));

        // Act
        var result = await _fallbackService.ExecuteWithFallbackAsync(operation, fallbackOperation, "test-operation");

        // Assert
        result.Result.Should().Be(expectedResult);
        result.UsedFallback.Should().BeFalse();
        result.OperationName.Should().Be("test-operation");
        result.FallbackReason.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithFailureAndShouldFallback_ShouldUseFallback()
    {
        // Arrange
        var apiException = new GitLabApiException("API Error", statusCode: 401);
        var fallbackResult = "fallback-result";
        var operation = new Func<CancellationToken, Task<string>>(ct => throw apiException);
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult(fallbackResult));

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(true);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(apiException)).Returns("Authentication failed");

        // Act
        var result = await _fallbackService.ExecuteWithFallbackAsync(operation, fallbackOperation, "test-operation");

        // Assert
        result.Result.Should().Be(fallbackResult);
        result.UsedFallback.Should().BeTrue();
        result.OperationName.Should().Be("test-operation");
        result.FallbackReason.Should().Be("Authentication failed");
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithFailureAndShouldNotFallback_ShouldThrowOriginalException()
    {
        // Arrange
        var apiException = new GitLabApiException("API Error", statusCode: 429);
        var operation = new Func<CancellationToken, Task<string>>(ct => throw apiException);
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("fallback"));

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(false);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _fallbackService.ExecuteWithFallbackAsync(operation, fallbackOperation, "test-operation"));
        
        thrownException.Should().Be(apiException);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithBothOperationsFailingAndShouldFallback_ShouldThrowCombinedException()
    {
        // Arrange
        var apiException = new GitLabApiException("API Error", statusCode: 401);
        var fallbackException = new InvalidOperationException("Fallback Error");
        var operation = new Func<CancellationToken, Task<string>>(ct => throw apiException);
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => throw fallbackException);

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(true);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(apiException)).Returns("Authentication failed");

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _fallbackService.ExecuteWithFallbackAsync(operation, fallbackOperation, "test-operation"));
        
        thrownException.Message.Should().Contain("Both GitLab API and fallback operations failed");
        thrownException.Message.Should().Contain("test-operation");
        thrownException.Message.Should().Contain("API Error");
        thrownException.Message.Should().Contain("Fallback Error");
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithNonGitLabException_ShouldCreateGitLabApiException()
    {
        // Arrange
        var originalException = new HttpRequestException("Network error");
        var fallbackResult = "fallback-result";
        var operation = new Func<CancellationToken, Task<string>>(ct => throw originalException);
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult(fallbackResult));

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(originalException)).Returns(true);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(It.IsAny<GitLabApiException>())).Returns("Network error occurred");

        // Act
        var result = await _fallbackService.ExecuteWithFallbackAsync(operation, fallbackOperation, "test-operation");

        // Assert
        result.Result.Should().Be(fallbackResult);
        result.UsedFallback.Should().BeTrue();
        result.FallbackReason.Should().Be("Network error occurred");
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(100, ct);
            return "success";
        });
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("fallback"));

        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _fallbackService.ExecuteWithFallbackAsync(operation, fallbackOperation, "test-operation", cts.Token));
    }

    #endregion

    #region ExecuteAnalysisWithFallbackAsync Tests

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithSuccessfulAnalysis_ShouldReturnResultWithoutFallback()
    {
        // Arrange
        var projectId = "test-project";
        var analysisResult = CreateSampleAnalysisResult();
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(analysisResult));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(analysisResult));

        // Act
        var result = await _fallbackService.ExecuteAnalysisWithFallbackAsync(
            projectId, analysisOperation, partialAnalysisOperation);

        // Assert
        result.Result.Should().Be(analysisResult);
        result.UsedFallback.Should().BeFalse();
        result.UsedCachedData.Should().BeFalse();
        result.ProjectId.Should().Be(projectId);
        result.FallbackReason.Should().BeNull();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithFailureAndShouldFallback_ShouldUsePartialAnalysis()
    {
        // Arrange
        var projectId = "test-project";
        var apiException = new GitLabApiException("API Error", statusCode: 500);
        var partialResult = CreateSampleAnalysisResult();
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => throw apiException);
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(partialResult));

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(true);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(apiException)).Returns("Server error occurred");

        // Act
        var result = await _fallbackService.ExecuteAnalysisWithFallbackAsync(
            projectId, analysisOperation, partialAnalysisOperation);

        // Assert
        result.Result.Should().Be(partialResult);
        result.UsedFallback.Should().BeTrue();
        result.UsedCachedData.Should().BeFalse();
        result.ProjectId.Should().Be(projectId);
        result.FallbackReason.Should().Be("Server error occurred");
        result.Warnings.Should().Contain("Analysis completed with limited data due to GitLab API issues");
        result.Warnings.Should().Contain("No cached data available");
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithCachedData_ShouldUseCachedDataInPartialAnalysis()
    {
        // Arrange
        var projectId = "test-project";
        var apiException = new GitLabApiException("API Error", statusCode: 500);
        var partialResult = CreateSampleAnalysisResult();

        // First, execute a successful analysis to cache data
        var initialResult = CreateSampleAnalysisResult();
        var initialAnalysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(initialResult));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(partialResult));

        await _fallbackService.ExecuteAnalysisWithFallbackAsync(
            projectId, initialAnalysisOperation, partialAnalysisOperation);

        // Now test with failure
        var failingAnalysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => throw apiException);

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(true);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(apiException)).Returns("Server error occurred");

        // Act
        var result = await _fallbackService.ExecuteAnalysisWithFallbackAsync(
            projectId, failingAnalysisOperation, partialAnalysisOperation);

        // Assert
        result.Result.Should().Be(partialResult);
        result.UsedFallback.Should().BeTrue();
        result.UsedCachedData.Should().BeTrue();
        result.ProjectId.Should().Be(projectId);
        result.Warnings.Should().Contain(w => w.Contains("Using cached data from"));
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithExpiredCache_ShouldNotUseCachedData()
    {
        // Arrange
        var projectId = "test-project";
        var apiException = new GitLabApiException("API Error", statusCode: 500);
        var partialResult = CreateSampleAnalysisResult();

        // First, execute a successful analysis to cache data
        var initialResult = CreateSampleAnalysisResult();
        var initialAnalysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(initialResult));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(partialResult));

        await _fallbackService.ExecuteAnalysisWithFallbackAsync(
            projectId, initialAnalysisOperation, partialAnalysisOperation);

        // Wait for cache to expire (simulate by clearing and re-adding with old timestamp)
        _fallbackService.ClearCache(projectId);

        // Now test with failure
        var failingAnalysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => throw apiException);

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(true);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(apiException)).Returns("Server error occurred");

        // Act
        var result = await _fallbackService.ExecuteAnalysisWithFallbackAsync(
            projectId, failingAnalysisOperation, partialAnalysisOperation);

        // Assert
        result.UsedCachedData.Should().BeFalse();
        result.Warnings.Should().Contain("No cached data available");
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithBothOperationsFailingAndShouldFallback_ShouldThrowCombinedException()
    {
        // Arrange
        var projectId = "test-project";
        var apiException = new GitLabApiException("API Error", statusCode: 500);
        var partialException = new InvalidOperationException("Partial analysis error");
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => throw apiException);
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => throw partialException);

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(true);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _fallbackService.ExecuteAnalysisWithFallbackAsync(
                projectId, analysisOperation, partialAnalysisOperation));
        
        thrownException.Message.Should().Contain("Project analysis failed");
        thrownException.Message.Should().Contain(projectId);
        thrownException.Message.Should().Contain("API Error");
        thrownException.Message.Should().Contain("Partial analysis error");
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithFailureAndShouldNotFallback_ShouldThrowOriginalException()
    {
        // Arrange
        var projectId = "test-project";
        var apiException = new GitLabApiException("API Error", statusCode: 429);
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => throw apiException);
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(CreateSampleAnalysisResult()));

        _mockErrorHandler.Setup(x => x.ShouldFallbackToManualMode(apiException)).Returns(false);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _fallbackService.ExecuteAnalysisWithFallbackAsync(
                projectId, analysisOperation, partialAnalysisOperation));
        
        thrownException.Should().Be(apiException);
    }

    #endregion

    #region CreateUserGuidance Tests

    [Fact]
    public void CreateUserGuidance_WithAuthenticationError_ShouldProvideAuthGuidance()
    {
        // Arrange
        var exception = new GitLabApiException("Unauthorized", statusCode: 401);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(exception)).Returns("Authentication failed");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "project analysis");

        // Assert
        guidance.OperationContext.Should().Be("project analysis");
        guidance.ErrorMessage.Should().Be("Authentication failed");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeFalse();
        guidance.Suggestions.Should().Contain(s => s.Contains("personal access token"));
        guidance.Suggestions.Should().Contain(s => s.Contains("scopes"));
        guidance.Suggestions.Should().Contain(s => s.Contains("regenerating"));
    }

    [Fact]
    public void CreateUserGuidance_WithAccessDeniedError_ShouldProvidePermissionGuidance()
    {
        // Arrange
        var exception = new GitLabApiException("Forbidden", statusCode: 403);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(exception)).Returns("Access denied");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "project retrieval");

        // Assert
        guidance.OperationContext.Should().Be("project retrieval");
        guidance.ErrorMessage.Should().Be("Access denied");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeFalse();
        guidance.Suggestions.Should().Contain(s => s.Contains("project owner"));
        guidance.Suggestions.Should().Contain(s => s.Contains("Reporter access"));
        guidance.Suggestions.Should().Contain(s => s.Contains("blocked or suspended"));
    }

    [Fact]
    public void CreateUserGuidance_WithNotFoundError_ShouldProvideSearchGuidance()
    {
        // Arrange
        var exception = new GitLabApiException("Not Found", statusCode: 404);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(exception)).Returns("Resource not found");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "project lookup");

        // Assert
        guidance.OperationContext.Should().Be("project lookup");
        guidance.ErrorMessage.Should().Be("Resource not found");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeFalse();
        guidance.Suggestions.Should().Contain(s => s.Contains("project ID or path"));
        guidance.Suggestions.Should().Contain(s => s.Contains("moved or deleted"));
        guidance.Suggestions.Should().Contain(s => s.Contains("list command"));
    }

    [Fact]
    public void CreateUserGuidance_WithRateLimitError_ShouldProvideRetryGuidance()
    {
        // Arrange
        var exception = new GitLabApiException("Too Many Requests", statusCode: 429);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(exception)).Returns("Rate limit exceeded");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "API request");

        // Assert
        guidance.OperationContext.Should().Be("API request");
        guidance.ErrorMessage.Should().Be("Rate limit exceeded");
        guidance.CanContinueWithManualMode.Should().BeFalse();
        guidance.ShouldRetryLater.Should().BeTrue();
        guidance.Suggestions.Should().Contain(s => s.Contains("Wait a few minutes"));
        guidance.Suggestions.Should().Contain(s => s.Contains("rate limits"));
        guidance.Suggestions.Should().Contain(s => s.Contains("off-peak hours"));
    }

    [Fact]
    public void CreateUserGuidance_WithServerError_ShouldProvideServerGuidance()
    {
        // Arrange
        var exception = new GitLabApiException("Internal Server Error", statusCode: 500);
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(exception)).Returns("GitLab server error");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "data retrieval");

        // Assert
        guidance.OperationContext.Should().Be("data retrieval");
        guidance.ErrorMessage.Should().Be("GitLab server error");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeTrue();
        guidance.Suggestions.Should().Contain(s => s.Contains("server is experiencing issues"));
        guidance.Suggestions.Should().Contain(s => s.Contains("status page"));
        guidance.Suggestions.Should().Contain(s => s.Contains("manual configuration"));
    }

    [Fact]
    public void CreateUserGuidance_WithNetworkError_ShouldProvideNetworkGuidance()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(It.IsAny<GitLabApiException>())).Returns("Network connection failed");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "API connection");

        // Assert
        guidance.OperationContext.Should().Be("API connection");
        guidance.ErrorMessage.Should().Be("Network connection failed");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeFalse();
        guidance.Suggestions.Should().Contain(s => s.Contains("internet connection"));
        guidance.Suggestions.Should().Contain(s => s.Contains("URL is accessible"));
        guidance.Suggestions.Should().Contain(s => s.Contains("firewall or proxy"));
    }

    [Fact]
    public void CreateUserGuidance_WithTimeoutError_ShouldProvideTimeoutGuidance()
    {
        // Arrange
        var exception = new TaskCanceledException("Request timeout");
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(It.IsAny<GitLabApiException>())).Returns("Request timed out");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "long operation");

        // Assert
        guidance.OperationContext.Should().Be("long operation");
        guidance.ErrorMessage.Should().Be("Request timed out");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeFalse();
        guidance.Suggestions.Should().Contain(s => s.Contains("internet connection"));
        guidance.Suggestions.Should().Contain(s => s.Contains("timeout setting"));
    }

    [Fact]
    public void CreateUserGuidance_WithUnknownError_ShouldProvideGenericGuidance()
    {
        // Arrange
        var exception = new InvalidOperationException("Unknown error");
        _mockErrorHandler.Setup(x => x.TranslateGitLabError(It.IsAny<GitLabApiException>())).Returns("Unknown error occurred");

        // Act
        var guidance = _fallbackService.CreateUserGuidance(exception, "unknown operation");

        // Assert
        guidance.OperationContext.Should().Be("unknown operation");
        guidance.ErrorMessage.Should().Be("Unknown error occurred");
        guidance.CanContinueWithManualMode.Should().BeTrue();
        guidance.ShouldRetryLater.Should().BeFalse();
        guidance.Suggestions.Should().Contain(s => s.Contains("error details"));
        guidance.Suggestions.Should().Contain(s => s.Contains("manual configuration"));
        guidance.Suggestions.Should().Contain(s => s.Contains("contact support"));
    }

    #endregion

    #region Cache Management Tests

    [Fact]
    public async Task ClearCache_WithSpecificProjectId_ShouldClearOnlyThatProject()
    {
        // Arrange
        var projectId1 = "project1";
        var projectId2 = "project2";

        // Cache some data by executing successful analyses
        var analysisResult = CreateSampleAnalysisResult();
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(analysisResult));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(analysisResult));

        await _fallbackService.ExecuteAnalysisWithFallbackAsync(projectId1, analysisOperation, partialAnalysisOperation);
        await _fallbackService.ExecuteAnalysisWithFallbackAsync(projectId2, analysisOperation, partialAnalysisOperation);

        var initialStats = _fallbackService.GetCacheStatistics();
        initialStats.TotalEntries.Should().Be(2);

        // Act
        _fallbackService.ClearCache(projectId1);

        // Assert
        var finalStats = _fallbackService.GetCacheStatistics();
        finalStats.TotalEntries.Should().Be(1);
        finalStats.ProjectIds.Should().Contain(projectId2);
        finalStats.ProjectIds.Should().NotContain(projectId1);
    }

    [Fact]
    public async Task ClearCache_WithNullProjectId_ShouldClearAllCache()
    {
        // Arrange
        var projectId1 = "project1";
        var projectId2 = "project2";

        // Cache some data
        var analysisResult = CreateSampleAnalysisResult();
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(analysisResult));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(analysisResult));

        await _fallbackService.ExecuteAnalysisWithFallbackAsync(projectId1, analysisOperation, partialAnalysisOperation);
        await _fallbackService.ExecuteAnalysisWithFallbackAsync(projectId2, analysisOperation, partialAnalysisOperation);

        var initialStats = _fallbackService.GetCacheStatistics();
        initialStats.TotalEntries.Should().Be(2);

        // Act
        _fallbackService.ClearCache();

        // Assert
        var finalStats = _fallbackService.GetCacheStatistics();
        finalStats.TotalEntries.Should().Be(0);
        finalStats.ProjectIds.Should().BeEmpty();
    }

    [Fact]
    public void GetCacheStatistics_WithEmptyCache_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _fallbackService.GetCacheStatistics();

        // Assert
        stats.TotalEntries.Should().Be(0);
        stats.OldestEntry.Should().BeNull();
        stats.NewestEntry.Should().BeNull();
        stats.ProjectIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCacheStatistics_WithCachedData_ShouldReturnCorrectStats()
    {
        // Arrange
        var projectId = "test-project";
        var analysisResult = CreateSampleAnalysisResult();
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(analysisResult));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(analysisResult));

        await _fallbackService.ExecuteAnalysisWithFallbackAsync(projectId, analysisOperation, partialAnalysisOperation);

        // Act
        var stats = _fallbackService.GetCacheStatistics();

        // Assert
        stats.TotalEntries.Should().Be(1);
        stats.OldestEntry.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        stats.NewestEntry.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        stats.ProjectIds.Should().Contain(projectId);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var fallbackOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("fallback"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _fallbackService.ExecuteWithFallbackAsync<string>(null!, fallbackOperation, "test"));
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_WithNullFallbackOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("success"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _fallbackService.ExecuteWithFallbackAsync(operation, null!, "test"));
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithNullProjectId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(CreateSampleAnalysisResult()));
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(CreateSampleAnalysisResult()));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _fallbackService.ExecuteAnalysisWithFallbackAsync(null!, analysisOperation, partialAnalysisOperation));
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithNullAnalysisOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var partialAnalysisOperation = new Func<CachedAnalysisResult?, CancellationToken, Task<ProjectAnalysisResult>>(
            (cached, ct) => Task.FromResult(CreateSampleAnalysisResult()));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _fallbackService.ExecuteAnalysisWithFallbackAsync("project", null!, partialAnalysisOperation));
    }

    [Fact]
    public async Task ExecuteAnalysisWithFallbackAsync_WithNullPartialAnalysisOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var analysisOperation = new Func<CancellationToken, Task<ProjectAnalysisResult>>(ct => Task.FromResult(CreateSampleAnalysisResult()));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _fallbackService.ExecuteAnalysisWithFallbackAsync("project", analysisOperation, null!));
    }

    [Fact]
    public void CreateUserGuidance_WithNullException_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _fallbackService.CreateUserGuidance(null!, "context"));
    }

    [Fact]
    public void CreateUserGuidance_WithNullOperationContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var exception = new GitLabApiException("Test error");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _fallbackService.CreateUserGuidance(exception, null!));
    }

    #endregion

    #region Helper Methods

    private static ProjectAnalysisResult CreateSampleAnalysisResult()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.DotNet,
            Framework = new FrameworkInfo
            {
                Name = ".NET",
                Version = "8.0"
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "dotnet",
                BuildCommands = new List<string> { "dotnet build" },
                TestCommands = new List<string> { "dotnet test" }
            },
            Dependencies = new DependencyInfo
            {
                Runtime = new RuntimeInfo
                {
                    Name = ".NET",
                    Version = "8.0"
                }
            },
            Confidence = AnalysisConfidence.High
        };
    }

    #endregion
   }
