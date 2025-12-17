namespace GitlabPipelineGenerator.Core.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value of a failed result</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result");

    /// <summary>
    /// Gets the error message
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing error of a successful result</exception>
    public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access error of a successful result");

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <param name="value">The success value</param>
    /// <returns>A successful result</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result<T> OnFailure(Action<string> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Maps the result to a new type if successful
    /// </summary>
    /// <typeparam name="TNew">The new type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A new result with the mapped value</returns>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess ? Result<TNew>.Success(mapper(Value)) : Result<TNew>.Failure(Error);
    }

    /// <summary>
    /// Binds the result to a new result-returning function if successful
    /// </summary>
    /// <typeparam name="TNew">The new type</typeparam>
    /// <param name="binder">The binding function</param>
    /// <returns>A new result</returns>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess ? binder(Value) : Result<TNew>.Failure(Error);
    }

    /// <summary>
    /// Gets the value or a default value if the result is a failure
    /// </summary>
    /// <param name="defaultValue">The default value</param>
    /// <returns>The value or default value</returns>
    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess ? Value : defaultValue;
    }

    /// <summary>
    /// Gets the value or the result of a function if the result is a failure
    /// </summary>
    /// <param name="defaultValueFactory">The default value factory</param>
    /// <returns>The value or default value</returns>
    public T GetValueOrDefault(Func<T> defaultValueFactory)
    {
        return IsSuccess ? Value : defaultValueFactory();
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Represents the result of an operation that can succeed or fail without a return value
/// </summary>
public readonly struct Result
{
    private readonly string? _error;

    private Result(string error)
    {
        _error = error;
        IsSuccess = false;
    }

    private Result(bool success)
    {
        _error = null;
        IsSuccess = success;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing error of a successful result</exception>
    public string Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access error of a successful result");

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string error) => new(error);

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result OnFailure(Action<string> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Maps the result to a typed result if successful
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A new typed result</returns>
    public Result<T> Map<T>(Func<T> mapper)
    {
        return IsSuccess ? Result<T>.Success(mapper()) : Result<T>.Failure(Error);
    }

    /// <summary>
    /// Binds the result to a new result-returning function if successful
    /// </summary>
    /// <param name="binder">The binding function</param>
    /// <returns>A new result</returns>
    public Result Bind(Func<Result> binder)
    {
        return IsSuccess ? binder() : this;
    }

    /// <summary>
    /// Binds the result to a new typed result-returning function if successful
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="binder">The binding function</param>
    /// <returns>A new typed result</returns>
    public Result<T> Bind<T>(Func<Result<T>> binder)
    {
        return IsSuccess ? binder() : Result<T>.Failure(Error);
    }
}