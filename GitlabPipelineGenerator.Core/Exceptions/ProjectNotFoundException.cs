namespace GitlabPipelineGenerator.Core.Exceptions;

/// <summary>
/// Exception thrown when a GitLab project is not found or not accessible
/// </summary>
public class ProjectNotFoundException : GitLabApiException
{
    /// <summary>
    /// Project identifier that was not found
    /// </summary>
    public string ProjectIdentifier { get; }

    /// <summary>
    /// Initializes a new instance of ProjectNotFoundException
    /// </summary>
    /// <param name="projectIdentifier">Project ID or path that was not found</param>
    public ProjectNotFoundException(string projectIdentifier)
        : base($"Project '{projectIdentifier}' was not found or is not accessible")
    {
        ProjectIdentifier = projectIdentifier;
    }

    /// <summary>
    /// Initializes a new instance of ProjectNotFoundException with a custom message
    /// </summary>
    /// <param name="projectIdentifier">Project ID or path that was not found</param>
    /// <param name="message">Custom error message</param>
    public ProjectNotFoundException(string projectIdentifier, string message)
        : base(message)
    {
        ProjectIdentifier = projectIdentifier;
    }

    /// <summary>
    /// Initializes a new instance of ProjectNotFoundException with a custom message and inner exception
    /// </summary>
    /// <param name="projectIdentifier">Project ID or path that was not found</param>
    /// <param name="message">Custom error message</param>
    /// <param name="innerException">Inner exception</param>
    public ProjectNotFoundException(string projectIdentifier, string message, Exception innerException)
        : base(message, innerException)
    {
        ProjectIdentifier = projectIdentifier;
    }
}