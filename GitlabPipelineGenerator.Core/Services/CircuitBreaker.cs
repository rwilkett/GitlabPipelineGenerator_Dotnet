using GitlabPipelineGenerator.Core.Exceptions;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Circuit breaker implementation for GitLab API operations
/// </summary>
public class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly object _lock = new();
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _nextAttemptTime = DateTime.MinValue;

    public CircuitBreaker(CircuitBreakerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Executes an operation through the circuit breaker
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await CheckStateAsync(cancellationToken);

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation through the circuit breaker without return value
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await CheckStateAsync(cancellationToken);

        try
        {
            await operation();
            OnSuccess();
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    private async Task CheckStateAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    // Normal operation
                    return;

                case CircuitBreakerState.Open:
                    // Check if we should transition to half-open
                    if (DateTime.UtcNow >= _nextAttemptTime)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        return;
                    }
                    break;

                case CircuitBreakerState.HalfOpen:
                    // Allow one request through
                    return;
            }
        }

        // If we reach here, circuit is open and not ready for retry
        throw new GitLabApiException(
            $"Circuit breaker is open. Service is temporarily unavailable. Next retry at: {_nextAttemptTime:yyyy-MM-dd HH:mm:ss} UTC");
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Failed during half-open, go back to open
                _state = CircuitBreakerState.Open;
                _nextAttemptTime = DateTime.UtcNow.Add(_options.OpenTimeout);
            }
            else if (_failureCount >= _options.FailureThreshold)
            {
                // Threshold exceeded, open the circuit
                _state = CircuitBreakerState.Open;
                _nextAttemptTime = DateTime.UtcNow.Add(_options.OpenTimeout);
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker to closed state
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _lastFailureTime = DateTime.MinValue;
            _nextAttemptTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Gets circuit breaker statistics
    /// </summary>
    public CircuitBreakerStats GetStats()
    {
        lock (_lock)
        {
            return new CircuitBreakerStats
            {
                State = _state,
                FailureCount = _failureCount,
                LastFailureTime = _lastFailureTime,
                NextAttemptTime = _nextAttemptTime
            };
        }
    }
}

/// <summary>
/// Circuit breaker configuration options
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of failures before opening the circuit
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Time to wait before attempting to close the circuit
    /// </summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Default options for GitLab API operations
    /// </summary>
    public static CircuitBreakerOptions Default => new()
    {
        FailureThreshold = 5,
        OpenTimeout = TimeSpan.FromMinutes(1)
    };

    /// <summary>
    /// Aggressive options for critical operations
    /// </summary>
    public static CircuitBreakerOptions Aggressive => new()
    {
        FailureThreshold = 3,
        OpenTimeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Conservative options for non-critical operations
    /// </summary>
    public static CircuitBreakerOptions Conservative => new()
    {
        FailureThreshold = 10,
        OpenTimeout = TimeSpan.FromMinutes(5)
    };
}

/// <summary>
/// Circuit breaker state enumeration
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed, requests flow normally
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, requests are blocked
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, testing if service is recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Circuit breaker statistics
/// </summary>
public class CircuitBreakerStats
{
    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public CircuitBreakerState State { get; set; }

    /// <summary>
    /// Current failure count
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Time of last failure
    /// </summary>
    public DateTime LastFailureTime { get; set; }

    /// <summary>
    /// Next time a retry will be attempted
    /// </summary>
    public DateTime NextAttemptTime { get; set; }
}