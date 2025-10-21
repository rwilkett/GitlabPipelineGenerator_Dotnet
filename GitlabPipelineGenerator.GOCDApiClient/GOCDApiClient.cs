using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GitlabPipelineGenerator.GOCDApiClient.Models;

namespace GitlabPipelineGenerator.GOCDApiClient;

public class GOCDApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public GOCDApiClient(string baseUrl, string bearerToken)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v1+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v10+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v2+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v6+json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public GOCDApiClient(string baseUrl, string username, string password)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v1+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v10+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v2+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v6+json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Pipeline>> GetPipelinesAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/pipelines");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<Pipeline>>(content, _jsonOptions);

        return apiResponse?.Embedded?.Pipelines ?? new List<Pipeline>();
    }

    public async Task<Pipeline?> GetPipelineAsync(string pipelineName)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/pipelines/{pipelineName}/status");

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Pipeline>(content, _jsonOptions);
    }

    public async Task<PipelineHistory> GetPipelineHistoryAsync(string pipelineName, int offset = 0)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/pipelines/{pipelineName}/history/{offset}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PipelineHistory>(content, _jsonOptions) ?? new PipelineHistory();
    }

    public async Task<List<Template>> GetTemplatesAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/admin/templates");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var templateList = JsonSerializer.Deserialize<TemplateList>(content, _jsonOptions);

        return templateList?.Embedded?.Templates ?? new List<Template>();
    }

    public async Task<Template?> GetTemplateAsync(string templateName)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/admin/templates/{templateName}");

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Template>(content, _jsonOptions);
    }

    public async Task<List<PipelineGroup>> GetPipelineGroupsAsync()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v1+json"));
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/admin/pipeline_groups");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var groupList = JsonSerializer.Deserialize<PipelineGroupList>(content, _jsonOptions);

        return groupList?.Embedded?.Groups ?? new List<PipelineGroup>();
    }

    public async Task<PipelineGroup?> GetPipelineGroupAsync(string groupName)
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v1+json"));
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/admin/pipeline_groups/{groupName}");

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PipelineGroup>(content, _jsonOptions);
    }

    public async Task<PipelineConfig?> GetPipelineConfigAsync(string pipelineName)
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.go.cd.v10+json"));
        var response = await _httpClient.GetAsync($"{_baseUrl}/go/api/admin/pipelines/{pipelineName}");

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PipelineConfig>(content, _jsonOptions);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}