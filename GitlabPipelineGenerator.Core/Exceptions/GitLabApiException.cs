namespace GitlabPipelineGenerator.Core.Exceptions;

/// <summary>
/// Base exception for GitLab API related errors
/// </summary>
public class GitLabApiException : Exception
{
    /// <summary>
    /// HTTP status code from the GitLab API response, if available
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// GitLab API error code, if available
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of GitLabApiException
    /// </summary>
    /// <param name="message">Error message</param>
    public GitLabApiException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of GitLabApiException with inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public GitLabApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of GitLabApiException with status code
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="statusCode">HTTP status code</param>
    public GitLabApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of GitLabApiException with status code and error code
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errorCode">GitLab API error code</param>
    public GitLabApiException(string message, int statusCode, string errorCode) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of GitLabApiException with all parameters
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="errorCode">GitLab API error code</param>
    /// <param name="innerException">Inner exception</param>
    public GitLabApiException(string message, int statusCode, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}