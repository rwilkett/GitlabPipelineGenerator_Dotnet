namespace GitlabPipelineGenerator.Core.Exceptions;

/// <summary>
/// Exception thrown when YAML serialization or deserialization fails
/// </summary>
public class YamlSerializationException : Exception
{
    /// <summary>
    /// Gets the YAML content that caused the serialization failure
    /// </summary>
    public string? YamlContent { get; }

    /// <summary>
    /// Gets the operation that failed (serialize, deserialize, validate)
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Gets the object that was being serialized (if applicable)
    /// </summary>
    public object? SourceObject { get; }

    /// <summary>
    /// Initializes a new instance of the YamlSerializationException class
    /// </summary>
    public YamlSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the YamlSerializationException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public YamlSerializationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the YamlSerializationException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public YamlSerializationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the YamlSerializationException class with detailed error information
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="yamlContent">The YAML content that caused the failure</param>
    public YamlSerializationException(string message, string operation, string? yamlContent) : base(message)
    {
        Operation = operation;
        YamlContent = yamlContent;
    }

    /// <summary>
    /// Initializes a new instance of the YamlSerializationException class with detailed error information for serialization
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="sourceObject">The object that was being serialized</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public YamlSerializationException(string message, string operation, object? sourceObject, Exception innerException) 
        : base(message, innerException)
    {
        Operation = operation;
        SourceObject = sourceObject;
    }

    /// <summary>
    /// Initializes a new instance of the YamlSerializationException class with detailed error information for deserialization
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="yamlContent">The YAML content that caused the failure</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public YamlSerializationException(string message, string operation, string? yamlContent, Exception innerException) 
        : base(message, innerException)
    {
        Operation = operation;
        YamlContent = yamlContent;
    }
}