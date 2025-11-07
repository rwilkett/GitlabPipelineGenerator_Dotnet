namespace GitlabPipelineGenerator.GitLabApiClient.Models;

public class Member
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AccessLevel { get; set; }
    public DateTime? ExpiresAt { get; set; }
}