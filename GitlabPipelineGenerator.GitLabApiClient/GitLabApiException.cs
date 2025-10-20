using System.Net;

namespace GitlabPipelineGenerator.GitLabApiClient;

public class GitLabApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public GitLabApiException(string message) : base(message) { }
    
    public GitLabApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
    
    public GitLabApiException(string message, Exception innerException) : base(message, innerException) { }
}