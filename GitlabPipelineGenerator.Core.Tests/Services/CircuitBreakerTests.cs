using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for CircuitBreaker
/// </summary>
public class CircuitBreakerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CircuitBreaker(null!));
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        // Arrange
        var options = CircuitBreakerOptions.Default;

        // Act
        var circuitBreaker = new CircuitBreaker(options);

        // Assert
        circuitBreaker.Should().NotBeNull();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public void State_InitialState_ShouldBeClosed()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);

        // Act & Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var expectedResult = "success";

        // Act
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult(expectedResult));

        // Assert
        result.Should().Be(expectedResult);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailuresUnderThreshold_ShouldRemainClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var circuitBreaker = new CircuitBreaker(options);

        // Act - Fail twice (under threshold)
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailuresAtThreshold_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3, OpenTimeout = TimeSpan.FromMilliseconds(100) };
        var circuitBreaker = new CircuitBreaker(options);

        // Act - Fail three times (at threshold)
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenCircuit_ShouldThrowCircuitBreakerException()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromSeconds(1) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<GitLabApiException>(
            () => circuitBreaker.ExecuteAsync(() => Task.FromResult("test")));
        
        exception.Message.Should().Contain("Circuit breaker is open");
        exception.Message.Should().Contain("temporarily unavailable");
    }

    [Fact]
    public async Task ExecuteAsync_WithOpenCircuitAfterTimeout_ShouldTransitionToHalfOpen()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Wait for timeout
        await Task.Delay(100);

        // Act - Next call should transition to half-open
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        // Assert
        result.Should().Be("success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithHalfOpenAndFailure_ShouldReturnToOpen()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Wait for timeout to transition to half-open
        await Task.Delay(100);

        // Act - Fail in half-open state
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task ExecuteAsync_WithHalfOpenAndSuccess_ShouldReturnToClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Wait for timeout to transition to half-open
        await Task.Delay(100);

        // Act - Succeed in half-open state
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        // Assert
        result.Should().Be("success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region ExecuteAsync Without Return Value Tests

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_WithSuccessfulOperation_ShouldComplete()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var executed = false;

        // Act
        await circuitBreaker.ExecuteAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        executed.Should().BeTrue();
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_WithFailure_ShouldThrowException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync(() => throw new InvalidOperationException("Test error")));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => circuitBreaker.ExecuteAsync(() => Task.FromResult("test"), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationDuringOperation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => circuitBreaker.ExecuteAsync(async () =>
            {
                cts.Cancel();
                await Task.Delay(100, cts.Token);
                return "test";
            }, cts.Token));
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_WithOpenCircuit_ShouldReturnToClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1 };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Act
        circuitBreaker.Reset();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task Reset_AfterReset_ShouldAllowNormalOperation()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1 };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Reset
        circuitBreaker.Reset();

        // Act
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        // Assert
        result.Should().Be("success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void GetStats_InitialState_ShouldReturnCorrectStats()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);

        // Act
        var stats = circuitBreaker.GetStats();

        // Assert
        stats.State.Should().Be(CircuitBreakerState.Closed);
        stats.FailureCount.Should().Be(0);
        stats.LastFailureTime.Should().Be(DateTime.MinValue);
        stats.NextAttemptTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task GetStats_AfterFailures_ShouldReturnCorrectStats()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var circuitBreaker = new CircuitBreaker(options);

        // Act - Fail twice
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));
        }

        var stats = circuitBreaker.GetStats();

        // Assert
        stats.State.Should().Be(CircuitBreakerState.Closed);
        stats.FailureCount.Should().Be(2);
        stats.LastFailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        stats.NextAttemptTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task GetStats_AfterCircuitOpens_ShouldReturnCorrectStats()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMinutes(1) };
        var circuitBreaker = new CircuitBreaker(options);

        // Act - Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        var stats = circuitBreaker.GetStats();

        // Assert
        stats.State.Should().Be(CircuitBreakerState.Open);
        stats.FailureCount.Should().Be(1);
        stats.LastFailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        stats.NextAttemptTime.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(1), TimeSpan.FromSeconds(1));
    }

    #endregion

    #region CircuitBreakerOptions Tests

    [Fact]
    public void CircuitBreakerOptions_Default_ShouldHaveExpectedValues()
    {
        // Act
        var options = CircuitBreakerOptions.Default;

        // Assert
        options.FailureThreshold.Should().Be(5);
        options.OpenTimeout.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CircuitBreakerOptions_Aggressive_ShouldHaveExpectedValues()
    {
        // Act
        var options = CircuitBreakerOptions.Aggressive;

        // Assert
        options.FailureThreshold.Should().Be(3);
        options.OpenTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CircuitBreakerOptions_Conservative_ShouldHaveExpectedValues()
    {
        // Act
        var options = CircuitBreakerOptions.Conservative;

        // Assert
        options.FailureThreshold.Should().Be(10);
        options.OpenTimeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task ExecuteAsync_WithMultipleSuccessesAfterFailures_ShouldResetFailureCount()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var circuitBreaker = new CircuitBreaker(options);

        // Fail twice
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));
        }

        // Act - Succeed once
        await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        var stats = circuitBreaker.GetStats();

        // Assert
        stats.FailureCount.Should().Be(0);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithExceptionInOperation_ShouldPropagateOriginalException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var originalException = new ArgumentException("Original error");

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<ArgumentException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw originalException));
        
        thrownException.Should().BeSameAs(originalException);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => circuitBreaker.ExecuteAsync<string>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => circuitBreaker.ExecuteAsync(null!));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ExecuteAsync_WithConcurrentOperations_ShouldMaintainConsistentState()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 5 };
        var circuitBreaker = new CircuitBreaker(options);
        var tasks = new List<Task>();

        // Act - Execute multiple operations concurrently
        for (int i = 0; i < 10; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (taskIndex % 2 == 0)
                    {
                        await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));
                    }
                    else
                    {
                        await circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error"));
                    }
                }
                catch
                {
                    // Expected for some operations
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Circuit breaker should be in a consistent state
        var stats = circuitBreaker.GetStats();
        stats.State.Should().BeOneOf(CircuitBreakerState.Closed, CircuitBreakerState.Open);
        stats.FailureCount.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Advanced State Transition Tests

    [Fact]
    public async Task ExecuteAsync_WithIntermittentFailures_ShouldNotOpenCircuitPrematurely()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var circuitBreaker = new CircuitBreaker(options);
        var callCount = 0;

        // Act - Alternate between success and failure
        for (int i = 0; i < 6; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(() =>
                {
                    callCount++;
                    if (i % 2 == 0) // Fail on even iterations
                        throw new InvalidOperationException("Test error");
                    return Task.FromResult("success");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected for failed iterations
            }
        }

        // Assert - Circuit should remain closed due to intermittent successes
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        var stats = circuitBreaker.GetStats();
        stats.FailureCount.Should().Be(0); // Should reset after each success
    }

    [Fact]
    public async Task ExecuteAsync_WithConsecutiveFailuresAfterSuccess_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 2 };
        var circuitBreaker = new CircuitBreaker(options);

        // Start with success
        await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        // Act - Now fail consecutively
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailureCountAtThresholdMinusOne_ShouldNotOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var circuitBreaker = new CircuitBreaker(options);

        // Act - Fail exactly threshold - 1 times
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        var stats = circuitBreaker.GetStats();
        stats.FailureCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithRapidStateTransitions_ShouldMaintainConsistency()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Wait for half-open
        await Task.Delay(100);

        // Rapid success -> failure -> success
        await circuitBreaker.ExecuteAsync(() => Task.FromResult("success")); // Should close
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error"))); // Should open
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    #endregion

    #region Timeout and Timing Tests

    [Fact]
    public async Task ExecuteAsync_WithVeryShortOpenTimeout_ShouldTransitionQuickly()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(10) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Act - Wait minimal time and try again
        await Task.Delay(20);
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        // Assert
        result.Should().Be("success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithLongOpenTimeout_ShouldRemainOpenForDuration()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromSeconds(1) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Act - Try before timeout expires
        await Task.Delay(100); // Much less than 1 second
        
        // Assert - Should still be blocked
        var exception = await Assert.ThrowsAsync<GitLabApiException>(
            () => circuitBreaker.ExecuteAsync(() => Task.FromResult("test")));
        exception.Message.Should().Contain("Circuit breaker is open");
    }

    [Fact]
    public async Task ExecuteAsync_WithExactTimeoutTiming_ShouldTransitionAtCorrectTime()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = timeout };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit and record time
        var openTime = DateTime.UtcNow;
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Wait for exact timeout
        var elapsed = DateTime.UtcNow - openTime;
        var remainingWait = timeout - elapsed;
        if (remainingWait > TimeSpan.Zero)
        {
            await Task.Delay(remainingWait + TimeSpan.FromMilliseconds(10)); // Small buffer
        }

        // Act - Should now allow transition to half-open
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        // Assert
        result.Should().Be("success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region Statistics and Monitoring Tests

    [Fact]
    public async Task GetStats_DuringStateTransitions_ShouldReflectCurrentState()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 2, OpenTimeout = TimeSpan.FromMilliseconds(50) };
        var circuitBreaker = new CircuitBreaker(options);

        // Initial state
        var initialStats = circuitBreaker.GetStats();
        initialStats.State.Should().Be(CircuitBreakerState.Closed);
        initialStats.FailureCount.Should().Be(0);

        // After first failure
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        var afterFirstFailure = circuitBreaker.GetStats();
        afterFirstFailure.State.Should().Be(CircuitBreakerState.Closed);
        afterFirstFailure.FailureCount.Should().Be(1);
        afterFirstFailure.LastFailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // After second failure (should open)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        var afterSecondFailure = circuitBreaker.GetStats();
        afterSecondFailure.State.Should().Be(CircuitBreakerState.Open);
        afterSecondFailure.FailureCount.Should().Be(2);
        afterSecondFailure.NextAttemptTime.Should().BeCloseTo(DateTime.UtcNow.Add(options.OpenTimeout), TimeSpan.FromSeconds(1));

        // After timeout (should allow half-open)
        await Task.Delay(100);
        
        // Trigger state check by attempting operation
        await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));

        var afterRecovery = circuitBreaker.GetStats();
        afterRecovery.State.Should().Be(CircuitBreakerState.Closed);
        afterRecovery.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_WithMultipleFailureTypes_ShouldTrackCorrectly()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var circuitBreaker = new CircuitBreaker(options);

        // Different types of failures
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Error 1")));

        await Assert.ThrowsAsync<ArgumentException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new ArgumentException("Error 2")));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new HttpRequestException("Error 3")));

        // Act
        var stats = circuitBreaker.GetStats();

        // Assert - Should track all failures regardless of type
        stats.State.Should().Be(CircuitBreakerState.Open);
        stats.FailureCount.Should().Be(3);
    }

    #endregion

    #region Performance and Load Tests

    [Fact]
    public async Task ExecuteAsync_WithHighConcurrency_ShouldMaintainThreadSafety()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 10 };
        var circuitBreaker = new CircuitBreaker(options);
        var successCount = 0;
        var failureCount = 0;
        var tasks = new List<Task>();

        // Act - Execute many operations concurrently
        for (int i = 0; i < 100; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (taskIndex % 3 == 0) // Fail every third operation
                    {
                        await circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error"));
                    }
                    else
                    {
                        await circuitBreaker.ExecuteAsync(() => Task.FromResult("success"));
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref failureCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should handle concurrent operations safely
        var stats = circuitBreaker.GetStats();
        (successCount + failureCount).Should().Be(100);
        stats.State.Should().BeOneOf(CircuitBreakerState.Closed, CircuitBreakerState.Open);
        
        // Failure count should be consistent with actual failures
        if (stats.State == CircuitBreakerState.Open)
        {
            stats.FailureCount.Should().BeGreaterOrEqualTo(options.FailureThreshold);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithRapidSuccessiveOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var results = new List<string>();

        // Act - Execute many rapid operations
        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            try
            {
                var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult($"result_{i}"));
                lock (results)
                {
                    results.Add(result);
                }
            }
            catch
            {
                // Some might fail due to timing
            }
        });

        await Task.WhenAll(tasks);

        // Assert - Should handle rapid operations without issues
        results.Should().HaveCount(50);
        results.Should().AllSatisfy(r => r.Should().StartWith("result_"));
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region Error Handling Edge Cases

    [Fact]
    public async Task ExecuteAsync_WithExceptionInSuccessCallback_ShouldStillUpdateState()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var callCount = 0;

        // Act & Assert - Exception in operation should still be tracked
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() =>
            {
                callCount++;
                throw new InvalidOperationException("Test error");
            }));

        // State should reflect the failure
        var stats = circuitBreaker.GetStats();
        stats.FailureCount.Should().Be(1);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithTaskCancellationInOperation_ShouldNotCountAsFailure()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var cts = new CancellationTokenSource();

        // Act & Assert
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => circuitBreaker.ExecuteAsync(() => Task.FromCanceled<string>(cts.Token), cts.Token));

        // Cancellation should not count as failure
        var stats = circuitBreaker.GetStats();
        stats.FailureCount.Should().Be(0);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithAggregateException_ShouldCountAsFailure()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(new CircuitBreakerOptions { FailureThreshold = 1 });

        // Act & Assert
        var innerException = new InvalidOperationException("Inner error");
        var aggregateException = new AggregateException(innerException);

        await Assert.ThrowsAsync<AggregateException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw aggregateException));

        // Should count as failure and open circuit
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    #endregion

    #region Reset and Recovery Tests

    [Fact]
    public async Task Reset_DuringHalfOpenState_ShouldReturnToClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1, OpenTimeout = TimeSpan.FromMilliseconds(50) };
        var circuitBreaker = new CircuitBreaker(options);

        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync<string>(() => throw new InvalidOperationException("Test error")));

        // Wait for half-open transition
        await Task.Delay(100);

        // Verify half-open state by checking that next operation would be allowed
        // (We can't directly set to half-open, but the next operation attempt will transition)

        // Act - Reset during half-open
        circuitBreaker.Reset();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        var stats = circuitBreaker.GetStats();
        stats.FailureCount.Should().Be(0);
        stats.LastFailureTime.Should().Be(DateTime.MinValue);
        stats.NextAttemptTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task Reset_WithPendingOperations_ShouldNotAffectInFlightOperations()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Default);
        var operationStarted = new TaskCompletionSource<bool>();
        var operationCanComplete = new TaskCompletionSource<bool>();

        // Start a long-running operation
        var operationTask = Task.Run(async () =>
        {
            return await circuitBreaker.ExecuteAsync(async () =>
            {
                operationStarted.SetResult(true);
                await operationCanComplete.Task;
                return "success";
            });
        });

        // Wait for operation to start
        await operationStarted.Task;

        // Act - Reset while operation is in progress
        circuitBreaker.Reset();

        // Complete the operation
        operationCanComplete.SetResult(true);
        var result = await operationTask;

        // Assert - Operation should complete successfully
        result.Should().Be("success");
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion
}
