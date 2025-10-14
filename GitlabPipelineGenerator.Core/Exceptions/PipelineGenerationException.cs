namespace GitlabPipelineGenerator.Core.Exceptions;

/// <summary>
/// Exception thrown when pipeline generation fails
/// </summary>
public class PipelineGenerationException : Exception
{
    /// <summary>
    /// Gets the pipeline options that caused the generation failure
    /// </summary>
    public object? PipelineOptions { get; }

    /// <summary>
    /// Gets the stage of generation where the failure occurred
    /// </summary>
    public string? GenerationStage { get; }

    /// <summary>
    /// Initializes a new instance of the PipelineGenerationException class
    /// </summary>
    public PipelineGenerationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the PipelineGenerationException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public PipelineGenerationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the PipelineGenerationException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public PipelineGenerationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the PipelineGenerationException class with detailed error information
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="pipelineOptions">The pipeline options that caused the failure</param>
    /// <param name="generationStage">The stage of generation where the failure occurred</param>
    public PipelineGenerationException(string message, object? pipelineOptions, string? generationStage) : base(message)
    {
        PipelineOptions = pipelineOptions;
        GenerationStage = generationStage;
    }

    /// <summary>
    /// Initializes a new instance of the PipelineGenerationException class with detailed error information and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="pipelineOptions">The pipeline options that caused the failure</param>
    /// <param name="generationStage">The stage of generation where the failure occurred</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public PipelineGenerationException(string message, object? pipelineOptions, string? generationStage, Exception innerException) 
        : base(message, innerException)
    {
        PipelineOptions = pipelineOptions;
        GenerationStage = generationStage;
    }
}