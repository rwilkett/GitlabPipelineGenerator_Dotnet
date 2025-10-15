using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Resilient wrapper for GitLab API operations with circuit breaker, retry, and timeout handling
/// </summary>
public class ResilientGitLabService
{
    private readonly IGitLabApiErrorHandler _errorHandler;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly TimeSpan _defaultTimeout;

    public ResilientGitLabService(
        IGitLabApiErrorHandler errorHandler,
        CircuitBreakerOptions? circuitBreakerOptions = null,
        TimeSpan? defaultTimeout = null)
    {
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _circuitBreaker = new CircuitBreaker(circuitBreakerOptions ?? CircuitBreakerOptions.Default);
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Executes a GitLab API operation with full resilience patterns
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="retryPolicy">Retry policy to use</param>
    /// <param name="timeout">Operation timeout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryPolicy? retryPolicy = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var effectiveRetryPolicy = retryPolicy ?? RetryPolicy.Default;

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _errorHandler.ExecuteWithRetryAsync(
                () => operation(combinedCts.Token),
                effectiveRetryPolicy,
                combinedCts.Token);
        }, combinedCts.Token);
    }

    /// <summary>
    /// Executes a GitLab API operation without return value with full resilience patterns
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="retryPolicy">Retry policy to use</param>
    /// <param name="timeout">Operation timeout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        RetryPolicy? retryPolicy = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        var effectiveRetryPolicy = retryPolicy ?? RetryPolicy.Default;

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        await _circuitBreaker.ExecuteAsync(async () =>
        {
            await _errorHandler.ExecuteWithRetryAsync(
                async () => { await operation(combinedCts.Token); return true; },
                effectiveRetryPolicy,
                combinedCts.Token);
        }, combinedCts.Token);
    }

    /// <summary>
    /// Executes an operation with partial failure handling - continues even if some operations fail
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="inputs">Collection of inputs to process</param>
    /// <param name="operation">Operation to execute for each input</param>
    /// <param name="continueOnFailure">Whether to continue processing if some operations fail</param>
    /// <param name="retryPolicy">Retry policy to use</param>
    /// <param name="timeout">Operation timeout per item</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results with success/failure information</returns>
    public async Task<PartialAnalysisResult<TResult>> ExecutePartialAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, CancellationToken, Task<TResult>> operation,
        bool continueOnFailure = true,
        RetryPolicy? retryPolicy = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<OperationResult<TResult>>();
        var exceptions = new List<Exception>();

        foreach (var input in inputs)
        {
            try
            {
                var result = await ExecuteAsync(
                    ct => operation(input, ct),
                    retryPolicy,
                    timeout,
                    cancellationToken);

                results.Add(new OperationResult<TResult>
                {
                    IsSuccess = true,
                    Result = result,
                    Input = input
                });
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                results.Add(new OperationResult<TResult>
                {
                    IsSuccess = false,
                    Exception = ex,
                    Input = input
                });

                if (!continueOnFailure)
                {
                    break;
                }
            }
        }

        return new PartialAnalysisResult<TResult>
        {
            Results = results,
            SuccessCount = results.Count(r => r.IsSuccess),
            FailureCount = results.Count(r => !r.IsSuccess),
            Exceptions = exceptions
        };
    }

    /// <summary>
    /// Gets circuit breaker statistics
    /// </summary>
    public CircuitBreakerStats GetCircuitBreakerStats()
    {
        return _circuitBreaker.GetStats();
    }

    /// <summary>
    /// Resets the circuit breaker
    /// </summary>
    public void ResetCircuitBreaker()
    {
        _circuitBreaker.Reset();
    }
}

/// <summary>
/// Result of a partial analysis operation
/// </summary>
/// <typeparam name="T">Result type</typeparam>
public class PartialAnalysisResult<T>
{
    /// <summary>
    /// All operation results
    /// </summary>
    public List<OperationResult<T>> Results { get; set; } = new();

    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// All exceptions that occurred
    /// </summary>
    public List<Exception> Exceptions { get; set; } = new();

    /// <summary>
    /// Whether any operations succeeded
    /// </summary>
    public bool HasAnySuccess => SuccessCount > 0;

    /// <summary>
    /// Whether all operations succeeded
    /// </summary>
    public bool AllSucceeded => FailureCount == 0;

    /// <summary>
    /// Successful results only
    /// </summary>
    public IEnumerable<T> SuccessfulResults => Results
        .Where(r => r.IsSuccess)
        .Select(r => r.Result!);
}

/// <summary>
/// Result of an individual operation
/// </summary>
/// <typeparam name="T">Result type</typeparam>
public class OperationResult<T>
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Result of the operation (if successful)
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Exception that occurred (if failed)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Original input that was processed
    /// </summary>
    public object? Input { get; set; }
}