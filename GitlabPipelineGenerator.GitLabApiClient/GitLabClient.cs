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

    public async Task<Group> GetGroupByPathAsync(string groupPath)
    {
        var encodedPath = Uri.EscapeDataString(groupPath);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{encodedPath}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get group by path: {response.StatusCode}", response.StatusCode);
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

    public async Task<List<Pipeline>> GetProjectPipelinesAsync(string projectIdOrPath, DateTime startDate, DateTime endDate)
    {
        var encodedPath = Uri.EscapeDataString(projectIdOrPath);
        var queryParams = new List<string>
        {
            $"updated_after={startDate:yyyy-MM-ddTHH:mm:ssZ}",
            $"updated_before={endDate:yyyy-MM-ddTHH:mm:ssZ}",
            "per_page=100"
        };
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects/{encodedPath}/pipelines?{query}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get project pipelines: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Pipeline>>(json, _jsonOptions) ?? new List<Pipeline>();
    }

    public async Task<List<Project>> SearchProjectsAsync(string search, int perPage = 20, int page = 1)
    {
        var queryParams = new List<string>
        {
            $"search={Uri.EscapeDataString(search)}",
            $"per_page={perPage}",
            $"page={page}"
        };
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/projects?{query}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to search projects: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Project>>(json, _jsonOptions) ?? new List<Project>();
    }

    public async Task<List<Group>> SearchGroupsAsync(string search, int perPage = 20, int page = 1)
    {
        var queryParams = new List<string>
        {
            $"search={Uri.EscapeDataString(search)}",
            $"per_page={perPage}",
            $"page={page}"
        };
        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups?{query}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to search groups: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Group>>(json, _jsonOptions) ?? new List<Group>();
    }

    public async Task<List<SamlGroupLink>> GetGroupSamlLinksAsync(string groupId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{groupId}/saml_group_links");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get SAML group links: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SamlGroupLink>>(json, _jsonOptions) ?? new List<SamlGroupLink>();
    }

    public async Task<List<Member>> GetGroupMembersAsync(string groupId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups/{groupId}/members");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get group members: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Member>>(json, _jsonOptions) ?? new List<Member>();
    }

    public async Task<List<Group>> GetTopLevelGroupsAsync(int perPage = 100)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/groups?top_level_only=true&per_page={perPage}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to get top-level groups: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Group>>(json, _jsonOptions) ?? new List<Group>();
    }

    public async Task<Group> CreateGroupAsync(string name, string path, string description)
    {
        var payload = new { name, path, description };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v4/groups", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to create group: {response.StatusCode}", response.StatusCode);
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Group>(responseJson, _jsonOptions) ?? throw new GitLabApiException("Failed to deserialize created group");
    }

    public async Task<Group> CreateSubgroupAsync(string parentId, string name, string path, string description)
    {
        var payload = new { name, path, description, parent_id = int.Parse(parentId) };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v4/groups", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to create subgroup: {response.StatusCode}", response.StatusCode);
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Group>(responseJson, _jsonOptions) ?? throw new GitLabApiException("Failed to deserialize created subgroup");
    }

    public async Task CreateGroupVariableAsync(string groupId, string key, string value, string variableType = "env_var", bool @protected = false, bool masked = false, string environmentScope = "*", string? description = null, bool raw = false, bool hidden = false)
    {
        // Ensure value is not null or empty and handle masked variables
        if (string.IsNullOrEmpty(value))
        {
            value = "placeholder_value";
        }
        
        // If variable is masked, we can't copy the actual value
        if (masked && value.Length > 0)
        {
            value = "masked_variable_placeholder";
        }
        
        var payload = new Dictionary<string, object>
        {
            ["key"] = key,
            ["value"] = value,
            ["variable_type"] = variableType,
            ["protected"] = @protected,
            ["raw"] = raw,
            ["environment_scope"] = environmentScope
        };
        
        // Use masked_and_hidden when both are true, otherwise use individual flags
        if (masked && hidden)
        {
            payload["masked_and_hidden"] = true;
        }
        else
        {
            payload["masked"] = masked;
            payload["hidden"] = hidden;
        }
        
        if (!string.IsNullOrEmpty(description))
        {
            payload["description"] = description;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v4/groups/{groupId}/variables", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var debugInfo = $"Payload: {json} | Error: {errorContent}";
            throw new GitLabApiException($"Failed to create group variable: {response.StatusCode} - {debugInfo}", response.StatusCode);
        }
    }

    public async Task CreateGroupSamlLinkAsync(string groupId, string samlGroupName, int accessLevel)
    {
        var payload = new { saml_group_name = samlGroupName, access_level = accessLevel };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v4/groups/{groupId}/saml_group_links", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to create SAML group link: {response.StatusCode}", response.StatusCode);
        }
    }

    public async Task DeleteAllGroupVariablesAsync(string groupId)
    {
        var variables = await GetGroupVariablesAsync(groupId);
        foreach (var variable in variables)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v4/groups/{groupId}/variables/{Uri.EscapeDataString(variable.Key)}");
            if (!response.IsSuccessStatusCode)
            {
                throw new GitLabApiException($"Failed to delete group variable: {response.StatusCode}", response.StatusCode);
            }
        }
    }

    public async Task DeleteAllGroupSamlLinksAsync(string groupId)
    {
        var samlLinks = await GetGroupSamlLinksAsync(groupId);
        foreach (var link in samlLinks)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v4/groups/{groupId}/saml_group_links/{Uri.EscapeDataString(link.SamlGroupName)}");
            if (!response.IsSuccessStatusCode)
            {
                throw new GitLabApiException($"Failed to delete SAML group link: {response.StatusCode}", response.StatusCode);
            }
        }
    }

    public async Task AddGroupMemberAsync(string groupId, int userId, int accessLevel)
    {
        var payload = new { user_id = userId, access_level = accessLevel };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v4/groups/{groupId}/members", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to add group member: {response.StatusCode}", response.StatusCode);
        }
    }

    public async Task RemoveGroupMemberAsync(string groupId, int userId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v4/groups/{groupId}/members/{userId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to remove group member: {response.StatusCode}", response.StatusCode);
        }
    }

    public async Task DeleteAllGroupMembersAsync(string groupId)
    {
        var members = await GetGroupMembersAsync(groupId);
        foreach (var member in members)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v4/groups/{groupId}/members/{member.Id}");
            if (!response.IsSuccessStatusCode)
            {
                throw new GitLabApiException($"Failed to delete group member: {response.StatusCode}", response.StatusCode);
            }
        }
    }

    public async Task<List<User>> SearchUsersAsync(string search)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v4/users?search={Uri.EscapeDataString(search)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new GitLabApiException($"Failed to search users: {response.StatusCode}", response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
    }

    public async Task CreateProjectVariableAsync(string projectIdOrPath, string key, string value, string variableType = "env_var", bool @protected = false, bool masked = false, string environmentScope = "*", string? description = null, bool raw = false, bool hidden = false)
    {
        var encodedPath = Uri.EscapeDataString(projectIdOrPath);
        
        // Ensure value is not null or empty and handle masked variables
        if (string.IsNullOrEmpty(value))
        {
            value = "placeholder_value";
        }
        
        // If variable is masked, we can't copy the actual value
        if (masked && value.Length > 0)
        {
            value = "masked_variable_placeholder";
        }
        
        var payload = new Dictionary<string, object>
        {
            ["key"] = key,
            ["value"] = value,
            ["variable_type"] = variableType,
            ["protected"] = @protected,
            ["raw"] = raw,
            ["environment_scope"] = environmentScope
        };
        
        // Use masked_and_hidden when both are true, otherwise use individual flags
        if (masked && hidden)
        {
            payload["masked_and_hidden"] = true;
        }
        else
        {
            payload["masked"] = masked;
            payload["hidden"] = hidden;
        }
        
        if (!string.IsNullOrEmpty(description))
        {
            payload["description"] = description;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v4/projects/{encodedPath}/variables", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            // Log the payload for debugging
            var debugInfo = $"Payload: {json} | Error: {errorContent}";
            throw new GitLabApiException($"Failed to create project variable: {response.StatusCode} - {debugInfo}", response.StatusCode);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

