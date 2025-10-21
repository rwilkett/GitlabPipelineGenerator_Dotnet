namespace GitlabPipelineGenerator.GOCDApiClient;

public class GOCDApiException : Exception
{
    public int StatusCode { get; }
    public string? ResponseContent { get; }

    public GOCDApiException(string message) : base(message)
    {
    }

    public GOCDApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public GOCDApiException(string message, int statusCode, string? responseContent = null) : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}