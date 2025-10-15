using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ResilientGitLabService
/// </summary>
public class ResilientGitLabServiceTests
{
    private readonly Mock<IGitLabApiErrorHandler> _mockErrorHandler;
    private readonly ResilientGitLabService _resilientService;

    public ResilientGitLabServiceTests()
    {
        _mockErrorHandler = new Mock<IGitLabApiErrorHandler>();
        _resilientService = new ResilientGitLabService(
            _mockErrorHandler.Object,
            CircuitBreakerOptions.Default,
            TimeSpan.FromSeconds(5));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullErrorHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ResilientGitLabService(null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = new ResilientGitLabService(_mockErrorHandler.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomOptions_ShouldCreateInstance()
    {
        // Arrange
        var customOptions = new CircuitBreakerOptions { FailureThreshold = 3 };
        var customTimeout = TimeSpan.FromSeconds(10);

        // Act
        var service = new ResilientGitLabService(_mockErrorHandler.Object, customOptions, customTimeout);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync With Return Value Tests

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        var operation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult(expectedResult));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecuteAsync(operation);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryableFailure_ShouldUseErrorHandler()
    {
        // Arrange
        var exception = new GitLabApiException("Server error", statusCode: 500);
        var operation = new Func<CancellationToken, Task<string>>(ct => throw exception);

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<GitLabApiException>(() => _resilientService.ExecuteAsync(operation));

        _mockErrorHandler.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomRetryPolicy_ShouldUseProvidedPolicy()
    {
        // Arrange
        var customPolicy = new RetryPolicy { MaxAttempts = 5 };
        var operation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("success"));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), customPolicy, It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        await _resilientService.ExecuteAsync(operation, customPolicy);

        // Assert
        _mockErrorHandler.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), customPolicy, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomTimeout_ShouldRespectTimeout()
    {
        // Arrange
        var customTimeout = TimeSpan.FromMilliseconds(100);
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(200, ct);
            return "success";
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _resilientService.ExecuteAsync(operation, timeout: customTimeout));
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(100, ct);
            return "success";
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _resilientService.ExecuteAsync(operation, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WithCircuitBreakerOpen_ShouldThrowCircuitBreakerException()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 1 });

        var operation = new Func<CancellationToken, Task<string>>(ct => throw new GitLabApiException("Error", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Open the circuit breaker
        await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));

        // Act & Assert - Next call should fail due to open circuit
        var exception = await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));
        exception.Message.Should().Contain("Circuit breaker is open");
    }

    #endregion

    #region ExecuteAsync Without Return Value Tests

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_WithSuccessfulOperation_ShouldComplete()
    {
        // Arrange
        var executed = false;
        var operation = new Func<CancellationToken, Task>(ct =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        await _resilientService.ExecuteAsync(operation);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_WithFailure_ShouldThrowException()
    {
        // Arrange
        var exception = new GitLabApiException("Error", statusCode: 500);
        var operation = new Func<CancellationToken, Task>(ct => throw exception);

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<GitLabApiException>(() => _resilientService.ExecuteAsync(operation));
    }

    #endregion

    #region ExecutePartialAsync Tests

    [Fact]
    public async Task ExecutePartialAsync_WithAllSuccessfulOperations_ShouldReturnAllResults()
    {
        // Arrange
        var inputs = new[] { "input1", "input2", "input3" };
        var operation = new Func<string, CancellationToken, Task<string>>(
            (input, ct) => Task.FromResult($"result_{input}"));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecutePartialAsync(inputs, operation);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
        result.SuccessfulResults.Should().HaveCount(3);
        result.SuccessfulResults.Should().Contain("result_input1", "result_input2", "result_input3");
    }

    [Fact]
    public async Task ExecutePartialAsync_WithSomeFailures_ShouldReturnPartialResults()
    {
        // Arrange
        var inputs = new[] { "input1", "input2", "input3" };
        var operation = new Func<string, CancellationToken, Task<string>>((input, ct) =>
        {
            if (input == "input2")
                throw new GitLabApiException("Error for input2", statusCode: 500);
            return Task.FromResult($"result_{input}");
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecutePartialAsync(inputs, operation, continueOnFailure: true);

        // Assert
        result.AllSucceeded.Should().BeFalse();
        result.HasAnySuccess.Should().BeTrue();
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(1);
        result.SuccessfulResults.Should().HaveCount(2);
        result.SuccessfulResults.Should().Contain("result_input1", "result_input3");
        result.Exceptions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutePartialAsync_WithContinueOnFailureFalse_ShouldStopOnFirstFailure()
    {
        // Arrange
        var inputs = new[] { "input1", "input2", "input3" };
        var operation = new Func<string, CancellationToken, Task<string>>((input, ct) =>
        {
            if (input == "input1")
                throw new GitLabApiException("Error for input1", statusCode: 500);
            return Task.FromResult($"result_{input}");
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecutePartialAsync(inputs, operation, continueOnFailure: false);

        // Assert
        result.AllSucceeded.Should().BeFalse();
        result.HasAnySuccess.Should().BeFalse();
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.Results.Should().HaveCount(1); // Should stop after first failure
        result.Exceptions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutePartialAsync_WithEmptyInputs_ShouldReturnEmptyResult()
    {
        // Arrange
        var inputs = Array.Empty<string>();
        var operation = new Func<string, CancellationToken, Task<string>>(
            (input, ct) => Task.FromResult($"result_{input}"));

        // Act
        var result = await _resilientService.ExecutePartialAsync(inputs, operation);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.Results.Should().BeEmpty();
        result.SuccessfulResults.Should().BeEmpty();
        result.Exceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutePartialAsync_WithCustomRetryPolicy_ShouldUseProvidedPolicy()
    {
        // Arrange
        var inputs = new[] { "input1" };
        var customPolicy = new RetryPolicy { MaxAttempts = 5 };
        var operation = new Func<string, CancellationToken, Task<string>>(
            (input, ct) => Task.FromResult($"result_{input}"));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), customPolicy, It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        await _resilientService.ExecutePartialAsync(inputs, operation, retryPolicy: customPolicy);

        // Assert
        _mockErrorHandler.Verify(
            x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), customPolicy, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Circuit Breaker Management Tests

    [Fact]
    public void GetCircuitBreakerStats_ShouldReturnCurrentStats()
    {
        // Act
        var stats = _resilientService.GetCircuitBreakerStats();

        // Assert
        stats.Should().NotBeNull();
        stats.State.Should().Be(CircuitBreakerState.Closed);
        stats.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCircuitBreakerStats_AfterFailures_ShouldReflectFailures()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 3 });

        var operation = new Func<CancellationToken, Task<string>>(ct => throw new GitLabApiException("Error", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Fail twice
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));
        }

        // Act
        var stats = service.GetCircuitBreakerStats();

        // Assert
        stats.State.Should().Be(CircuitBreakerState.Closed);
        stats.FailureCount.Should().Be(2);
    }

    [Fact]
    public void ResetCircuitBreaker_ShouldResetToClosedState()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 1 });

        // Act
        service.ResetCircuitBreaker();
        var stats = service.GetCircuitBreakerStats();

        // Assert
        stats.State.Should().Be(CircuitBreakerState.Closed);
        stats.FailureCount.Should().Be(0);
    }

    #endregion

    #region PartialAnalysisResult Tests

    [Fact]
    public void PartialAnalysisResult_HasAnySuccess_WithSuccesses_ShouldReturnTrue()
    {
        // Arrange
        var result = new PartialAnalysisResult<string>
        {
            SuccessCount = 2,
            FailureCount = 1
        };

        // Act & Assert
        result.HasAnySuccess.Should().BeTrue();
    }

    [Fact]
    public void PartialAnalysisResult_HasAnySuccess_WithoutSuccesses_ShouldReturnFalse()
    {
        // Arrange
        var result = new PartialAnalysisResult<string>
        {
            SuccessCount = 0,
            FailureCount = 3
        };

        // Act & Assert
        result.HasAnySuccess.Should().BeFalse();
    }

    [Fact]
    public void PartialAnalysisResult_AllSucceeded_WithNoFailures_ShouldReturnTrue()
    {
        // Arrange
        var result = new PartialAnalysisResult<string>
        {
            SuccessCount = 3,
            FailureCount = 0
        };

        // Act & Assert
        result.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public void PartialAnalysisResult_AllSucceeded_WithFailures_ShouldReturnFalse()
    {
        // Arrange
        var result = new PartialAnalysisResult<string>
        {
            SuccessCount = 2,
            FailureCount = 1
        };

        // Act & Assert
        result.AllSucceeded.Should().BeFalse();
    }

    [Fact]
    public void PartialAnalysisResult_SuccessfulResults_ShouldReturnOnlySuccessfulResults()
    {
        // Arrange
        var result = new PartialAnalysisResult<string>
        {
            Results = new List<OperationResult<string>>
            {
                new() { IsSuccess = true, Result = "success1" },
                new() { IsSuccess = false, Exception = new Exception("error") },
                new() { IsSuccess = true, Result = "success2" }
            }
        };

        // Act
        var successfulResults = result.SuccessfulResults.ToList();

        // Assert
        successfulResults.Should().HaveCount(2);
        successfulResults.Should().Contain("success1", "success2");
    }

    #endregion

    #region OperationResult Tests

    [Fact]
    public void OperationResult_WithSuccess_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var result = new OperationResult<string>
        {
            IsSuccess = true,
            Result = "test result",
            Input = "test input"
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().Be("test result");
        result.Exception.Should().BeNull();
        result.Input.Should().Be("test input");
    }

    [Fact]
    public void OperationResult_WithFailure_ShouldHaveCorrectProperties()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = new OperationResult<string>
        {
            IsSuccess = false,
            Exception = exception,
            Input = "test input"
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Result.Should().BeNull();
        result.Exception.Should().Be(exception);
        result.Input.Should().Be("test input");
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _resilientService.ExecuteAsync<string>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _resilientService.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecutePartialAsync_WithNullInputs_ShouldThrowArgumentNullException()
    {
        // Arrange
        var operation = new Func<string, CancellationToken, Task<string>>(
            (input, ct) => Task.FromResult($"result_{input}"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _resilientService.ExecutePartialAsync<string, string>(null!, operation));
    }

    [Fact]
    public async Task ExecutePartialAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var inputs = new[] { "input1" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _resilientService.ExecutePartialAsync<string, string>(inputs, null!));
    }

    #endregion

    #region Partial Analysis Advanced Scenarios

    [Fact]
    public async Task ExecutePartialAsync_WithMixedSuccessAndFailureRates_ShouldHandleCorrectly()
    {
        // Arrange
        var inputs = Enumerable.Range(1, 10).Select(i => $"input{i}").ToArray();
        var operation = new Func<string, CancellationToken, Task<string>>((input, ct) =>
        {
            // Fail every third input
            if (input.EndsWith("3") || input.EndsWith("6") || input.EndsWith("9"))
                throw new GitLabApiException($"Error for {input}", statusCode: 500);
            return Task.FromResult($"result_{input}");
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecutePartialAsync(inputs, operation, continueOnFailure: true);

        // Assert
        result.SuccessCount.Should().Be(7);
        result.FailureCount.Should().Be(3);
        result.HasAnySuccess.Should().BeTrue();
        result.AllSucceeded.Should().BeFalse();
        result.SuccessfulResults.Should().HaveCount(7);
        result.Exceptions.Should().HaveCount(3);
        
        // Verify specific results
        result.SuccessfulResults.Should().Contain("result_input1", "result_input2", "result_input4", "result_input5");
        result.Results.Where(r => !r.IsSuccess).Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecutePartialAsync_WithAllFailures_ShouldReturnAllFailureResults()
    {
        // Arrange
        var inputs = new[] { "input1", "input2", "input3" };
        var operation = new Func<string, CancellationToken, Task<string>>((input, ct) =>
            throw new GitLabApiException($"Error for {input}", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecutePartialAsync(inputs, operation, continueOnFailure: true);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(3);
        result.HasAnySuccess.Should().BeFalse();
        result.AllSucceeded.Should().BeFalse();
        result.SuccessfulResults.Should().BeEmpty();
        result.Exceptions.Should().HaveCount(3);
        result.Results.Should().AllSatisfy(r => r.IsSuccess.Should().BeFalse());
    }

    [Fact]
    public async Task ExecutePartialAsync_WithCircuitBreakerTripping_ShouldStopProcessing()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 2 });

        var inputs = new[] { "input1", "input2", "input3", "input4", "input5" };
        var operation = new Func<string, CancellationToken, Task<string>>((input, ct) =>
            throw new GitLabApiException($"Error for {input}", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Act
        var result = await service.ExecutePartialAsync(inputs, operation, continueOnFailure: true);

        // Assert - Should stop after circuit breaker opens
        result.FailureCount.Should().BeLessOrEqualTo(3); // 2 failures + 1 circuit breaker exception
        result.SuccessCount.Should().Be(0);
        result.Exceptions.Should().NotBeEmpty();
        
        // At least one exception should be circuit breaker related
        result.Exceptions.Should().Contain(ex => ex.Message.Contains("Circuit breaker is open"));
    }

    [Fact]
    public async Task ExecutePartialAsync_WithTimeoutOnSomeOperations_ShouldHandleTimeoutsCorrectly()
    {
        // Arrange
        var inputs = new[] { "fast", "slow", "medium" };
        var operation = new Func<string, CancellationToken, Task<string>>(async (input, ct) =>
        {
            var delay = input switch
            {
                "fast" => 10,
                "medium" => 50,
                "slow" => 200, // Will timeout with 100ms timeout
                _ => 10
            };
            await Task.Delay(delay, ct);
            return $"result_{input}";
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act
        var result = await _resilientService.ExecutePartialAsync(
            inputs, operation, continueOnFailure: true, timeout: TimeSpan.FromMilliseconds(100));

        // Assert
        result.SuccessCount.Should().Be(2); // fast and medium should succeed
        result.FailureCount.Should().Be(1); // slow should timeout
        result.SuccessfulResults.Should().Contain("result_fast", "result_medium");
        result.Exceptions.Should().ContainSingle().Which.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecutePartialAsync_WithDifferentRetryPoliciesPerOperation_ShouldRespectPolicy()
    {
        // Arrange
        var inputs = new[] { "retry", "no-retry" };
        var callCounts = new Dictionary<string, int>();
        var operation = new Func<string, CancellationToken, Task<string>>((input, ct) =>
        {
            callCounts[input] = callCounts.GetValueOrDefault(input, 0) + 1;
            
            if (input == "retry" && callCounts[input] <= 2)
                throw new GitLabApiException("Retryable error", statusCode: 500);
            if (input == "no-retry")
                throw new GitLabApiException("Non-retryable error", statusCode: 401);
                
            return Task.FromResult($"result_{input}");
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        var aggressivePolicy = new RetryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(1) };

        // Act
        var result = await _resilientService.ExecutePartialAsync(
            inputs, operation, continueOnFailure: true, retryPolicy: aggressivePolicy);

        // Assert
        callCounts["retry"].Should().Be(3); // Should succeed on third attempt
        callCounts["no-retry"].Should().Be(3); // Should retry even non-retryable (handled by error handler)
        result.SuccessCount.Should().Be(1); // Only "retry" should succeed
        result.FailureCount.Should().Be(1); // "no-retry" should fail
    }

    #endregion

    #region Circuit Breaker Integration Tests

    [Fact]
    public async Task ExecuteAsync_WithCircuitBreakerHalfOpen_ShouldAllowOneRequest()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) });

        var operation = new Func<CancellationToken, Task<string>>(ct => throw new GitLabApiException("Error", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Open the circuit
        await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));

        // Wait for half-open transition
        await Task.Delay(100);

        // Act - Should allow one request in half-open state
        var successOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("success"));
        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        var result = await service.ExecuteAsync(successOperation);

        // Assert
        result.Should().Be("success");
        service.GetCircuitBreakerStats().State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithCircuitBreakerHalfOpenAndFailure_ShouldReturnToOpen()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) });

        var operation = new Func<CancellationToken, Task<string>>(ct => throw new GitLabApiException("Error", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Open the circuit
        await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));

        // Wait for half-open transition
        await Task.Delay(100);

        // Act - Fail in half-open state
        await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));

        // Assert - Should return to open state
        service.GetCircuitBreakerStats().State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task ResetCircuitBreaker_AfterFailures_ShouldAllowImmediateRetry()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 1 });

        var operation = new Func<CancellationToken, Task<string>>(ct => throw new GitLabApiException("Error", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Open the circuit
        await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));
        service.GetCircuitBreakerStats().State.Should().Be(CircuitBreakerState.Open);

        // Act - Reset circuit breaker
        service.ResetCircuitBreaker();

        // Assert - Should allow immediate retry
        var successOperation = new Func<CancellationToken, Task<string>>(ct => Task.FromResult("success"));
        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        var result = await service.ExecuteAsync(successOperation);
        result.Should().Be("success");
        service.GetCircuitBreakerStats().State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region Timeout and Cancellation Advanced Tests

    [Fact]
    public async Task ExecuteAsync_WithVeryShortTimeout_ShouldTimeoutQuickly()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(1000, ct); // Long operation
            return "success";
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _resilientService.ExecuteAsync(operation, timeout: TimeSpan.FromMilliseconds(100)));
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationDuringRetry_ShouldCancelGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var callCount = 0;
        var operation = new Func<CancellationToken, Task<string>>(ct =>
        {
            callCount++;
            if (callCount == 1)
            {
                // Cancel after first attempt
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    cts.Cancel();
                });
            }
            throw new GitLabApiException("Error", statusCode: 500);
        });

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _resilientService.ExecuteAsync(operation, cancellationToken: cts.Token));

        callCount.Should().Be(1, "Should not retry after cancellation");
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutShorterThanRetryDelay_ShouldTimeoutBeforeRetry()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<CancellationToken, Task<string>>(ct =>
        {
            callCount++;
            throw new GitLabApiException("Error", statusCode: 500);
        });

        var longRetryPolicy = new RetryPolicy 
        { 
            MaxAttempts = 3, 
            BaseDelay = TimeSpan.FromSeconds(2) 
        };

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<string>>, RetryPolicy, CancellationToken>((op, policy, ct) => op());

        // Act & Assert
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _resilientService.ExecuteAsync(operation, longRetryPolicy, TimeSpan.FromMilliseconds(500)));
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        callCount.Should().Be(1, "Should timeout before retry delay completes");
    }

    #endregion

    #region Error Propagation and Wrapping Tests

    [Fact]
    public async Task ExecuteAsync_WithErrorHandlerThrowingDifferentException_ShouldPropagateNewException()
    {
        // Arrange
        var originalException = new GitLabApiException("Original error", statusCode: 500);
        var wrappedException = new GitLabApiException("Wrapped error", originalException);
        var operation = new Func<CancellationToken, Task<string>>(ct => throw originalException);

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(wrappedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<GitLabApiException>(
            () => _resilientService.ExecuteAsync(operation));

        thrownException.Should().Be(wrappedException);
        thrownException.InnerException.Should().Be(originalException);
    }

    [Fact]
    public async Task ExecuteAsync_WithCircuitBreakerAndErrorHandlerExceptions_ShouldPrioritizeCircuitBreakerException()
    {
        // Arrange
        var service = new ResilientGitLabService(
            _mockErrorHandler.Object,
            new CircuitBreakerOptions { FailureThreshold = 1 });

        var operation = new Func<CancellationToken, Task<string>>(ct => throw new GitLabApiException("Error", statusCode: 500));

        _mockErrorHandler
            .Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<RetryPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitLabApiException("Error", statusCode: 500));

        // Open the circuit
        await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));

        // Act & Assert - Next call should fail with circuit breaker exception
        var exception = await Assert.ThrowsAsync<GitLabApiException>(() => service.ExecuteAsync(operation));
        exception.Message.Should().Contain("Circuit breaker is open");
    }

    #endregion
}
