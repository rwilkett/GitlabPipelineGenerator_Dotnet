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

    public async Task<Group> GetGroupAsync(string groupId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{groupId}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get group: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Group>(json, _jsonOptions) ?? throw new GitLabApiException("Failed to deserialize group");
    }

    public async Task<List<Project>> GetGroupProjectsAsync(string groupId, int perPage = 20, int page = 1)
    {
        var queryParams = new List<string> { $"per_page={perPage}", $"page={page}" };
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{groupId}/projects?{query}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get group projects: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Project>>(json, _jsonOptions) ?? new List<Project>();
    }

    public async Task<List<Group>> GetSubgroupsAsync(string groupId, int perPage = 20, int page = 1)
    {
        var queryParams = new List<string> { $"per_page={perPage}", $"page={page}" };
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{groupId}/subgroups?{query}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get subgroups: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Group>>(json, _jsonOptions) ?? new List<Group>();
    }

    public async Task<List<ProjectVariable>> GetProjectVariablesAsync(string projectIdOrPath)
    {
        var encodedPath = Uri.EscapeDataString(projectIdOrPath);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects/{encodedPath}/variables");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get project variables: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProjectVariable>>(json, _jsonOptions) ?? new List<ProjectVariable>();
    }

    public async Task<List<GroupVariable>> GetGroupVariablesAsync(string groupId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{groupId}/variables");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get group variables: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GroupVariable>>(json, _jsonOptions) ?? new List<GroupVariable>();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

