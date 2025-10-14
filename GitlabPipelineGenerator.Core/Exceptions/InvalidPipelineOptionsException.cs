namespace GitlabPipelineGenerator.Core.Exceptions;

/// <summary>
/// Exception thrown when pipeline options are invalid
/// </summary>
public class InvalidPipelineOptionsException : Exception
{
    /// <summary>
    /// Gets the validation errors that caused the exception
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Gets the invalid pipeline options
    /// </summary>
    public object? PipelineOptions { get; }

    /// <summary>
    /// Initializes a new instance of the InvalidPipelineOptionsException class
    /// </summary>
    public InvalidPipelineOptionsException() : this(new List<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPipelineOptionsException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public InvalidPipelineOptionsException(string message) : base(message)
    {
        ValidationErrors = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPipelineOptionsException class with validation errors
    /// </summary>
    /// <param name="validationErrors">The validation errors</param>
    public InvalidPipelineOptionsException(IEnumerable<string> validationErrors) 
        : base($"Invalid pipeline options: {string.Join(", ", validationErrors)}")
    {
        ValidationErrors = validationErrors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPipelineOptionsException class with validation errors and pipeline options
    /// </summary>
    /// <param name="validationErrors">The validation errors</param>
    /// <param name="pipelineOptions">The invalid pipeline options</param>
    public InvalidPipelineOptionsException(IEnumerable<string> validationErrors, object? pipelineOptions) 
        : base($"Invalid pipeline options: {string.Join(", ", validationErrors)}")
    {
        ValidationErrors = validationErrors.ToList().AsReadOnly();
        PipelineOptions = pipelineOptions;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPipelineOptionsException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public InvalidPipelineOptionsException(string message, Exception innerException) : base(message, innerException)
    {
        ValidationErrors = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the InvalidPipelineOptionsException class with detailed error information
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="validationErrors">The validation errors</param>
    /// <param name="pipelineOptions">The invalid pipeline options</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public InvalidPipelineOptionsException(string message, IEnumerable<string> validationErrors, object? pipelineOptions, Exception innerException) 
        : base(message, innerException)
    {
        ValidationErrors = validationErrors.ToList().AsReadOnly();
        PipelineOptions = pipelineOptions;
    }
}