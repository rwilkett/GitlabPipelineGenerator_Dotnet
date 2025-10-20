using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GitlabPipelineGenerator.GitLabApiClient.Models;

namespace GitlabPipelineGenerator.GitLabApiClient;

public class GitLabClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitLabClient(string baseUrl, string accessToken)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitlabPipelineGenerator/1.0");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Project> GetProjectAsync(string projectIdOrPath)
    {
        var encodedPath = Uri.EscapeDataString(projectIdOrPath);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects/{encodedPath}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get project: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Project>(json, _jsonOptions) ?? throw new GitLabApiException("Failed to deserialize project");
    }

    public async Task<List<Project>> GetProjectsAsync(bool owned = false, int perPage = 20, int page = 1)
    {
        var queryParams = new List<string> { $"per_page={perPage}", $"page={page}" };
        if (owned) queryParams.Add("owned=true");
        
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects?{query}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get projects: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Project>>(json, _jsonOptions) ?? new List<Project>();
    }

    public async Task<List<RepositoryTree>> GetRepositoryTreeAsync(string projectIdOrPath, string? path = null, string? refParam = null)
    {
        var encodedProjectPath = Uri.EscapeDataString(projectIdOrPath);
        var queryParams = new List<string> { "recursive=true" };
        
        if (!string.IsNullOrEmpty(path)) queryParams.Add($"path={Uri.EscapeDataString(path)}");
        if (!string.IsNullOrEmpty(refParam)) queryParams.Add($"ref={Uri.EscapeDataString(refParam)}");
        
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects/{encodedProjectPath}/repository/tree?{query}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get repository tree: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<RepositoryTree>>(json, _jsonOptions) ?? new List<RepositoryTree>();
    }

    public async Task<RepositoryFile> GetFileAsync(string projectIdOrPath, string filePath, string? refParam = null)
    {
        var encodedProjectPath = Uri.EscapeDataString(projectIdOrPath);
        var encodedFilePath = Uri.EscapeDataString(filePath);
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(refParam)) queryParams.Add($"ref={Uri.EscapeDataString(refParam)}");
        
        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects/{encodedProjectPath}/repository/files/{encodedFilePath}{query}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get file: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        var file = JsonSerializer.Deserialize<RepositoryFile>(json, _jsonOptions) ?? throw new GitLabApiException("Failed to deserialize file");
        
        // Decode base64 content if needed
        if (file.Encoding == "base64" && !string.IsNullOrEmpty(file.Content))
        {
            file.Content = Encoding.UTF8.GetString(Convert.FromBase64String(file.Content));
        }
        
        return file;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public class GitLabApiException : Exception
{
    public System.Net.HttpStatusCode? StatusCode { get; }

    public GitLabApiException(string message) : base(message) { }
    
    public GitLabApiException(string message, System.Net.HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
    
    public GitLabApiException(string message, Exception innerException) : base(message, innerException) { }
}