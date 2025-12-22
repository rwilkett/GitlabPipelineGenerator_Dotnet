using System.Text.Json;
using GitlabPipelineGenerator.OctopusApiClient.Models;

namespace GitlabPipelineGenerator.OctopusApiClient;

public class OctopusApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public OctopusApiClient(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Octopus-ApiKey", apiKey);
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/projects");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ProjectsResponse>(json, _jsonOptions);
        return result?.Items ?? new List<Project>();
    }

    public async Task<List<Models.Environment>> GetEnvironmentsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/environments");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EnvironmentsResponse>(json, _jsonOptions);
        return result?.Items ?? new List<Models.Environment>();
    }

    public async Task<List<UserResource>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/all");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UserResource>>(json, _jsonOptions) ?? new List<UserResource>();
    }

    private class ProjectsResponse
    {
        public List<Project> Items { get; set; } = new();
    }

    private class EnvironmentsResponse
    {
        public List<Models.Environment> Items { get; set; } = new();
    }
}