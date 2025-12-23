using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Models;

public class User
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("login_name")]
    public string LoginName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("is_admin")]
    public bool Admin { get; set; }

    [JsonPropertyName("roles")]
    public List<Role> Roles { get; set; } = new();
}

public class Role
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class UserList
{
    [JsonPropertyName("_embedded")]
    public UserEmbedded? Embedded { get; set; }
}

public class UserEmbedded
{
    [JsonPropertyName("users")]
    public List<User> Users { get; set; } = new();
}