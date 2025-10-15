using GitlabPipelineGenerator.Core.Models.GitLab;
using GitLabApiClient;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service for handling GitLab API authentication and client management
/// </summary>
public interface IGitLabAuthenticationService
{
    /// <summary>
    /// Authenticates with GitLab API and returns a configured client
    /// </summary>
    /// <param name="options">Connection options including token and instance URL</param>
    /// <returns>Authenticated GitLab API client</returns>
    Task<GitLabClient> AuthenticateAsync(GitLabConnectionOptions options);

    /// <summary>
    /// Validates a GitLab personal access token
    /// </summary>
    /// <param name="token">Personal access token to validate</param>
    /// <param name="instanceUrl">GitLab instance URL (optional, defaults to gitlab.com)</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> ValidateTokenAsync(string token, string? instanceUrl = null);

    /// <summary>
    /// Gets information about the currently authenticated user
    /// </summary>
    /// <returns>User information</returns>
    Task<GitLabUserInfo> GetCurrentUserAsync();

    /// <summary>
    /// Stores GitLab credentials securely in the OS credential store
    /// </summary>
    /// <param name="options">Connection options to store</param>
    void StoreCredentials(GitLabConnectionOptions options);

    /// <summary>
    /// Loads stored GitLab credentials from the OS credential store
    /// </summary>
    /// <returns>Stored connection options, or null if none found</returns>
    GitLabConnectionOptions? LoadStoredCredentials();

    /// <summary>
    /// Clears stored GitLab credentials from the OS credential store
    /// </summary>
    void ClearStoredCredentials();

    /// <summary>
    /// Loads stored credentials for a specific profile
    /// </summary>
    /// <param name="profileName">Name of the profile to load</param>
    /// <returns>Stored connection options, or null if profile not found</returns>
    GitLabConnectionOptions? LoadStoredCredentials(string profileName);

    /// <summary>
    /// Stores credentials for a specific profile
    /// </summary>
    /// <param name="options">Connection options to store</param>
    /// <param name="profileName">Name of the profile</param>
    void StoreCredentials(GitLabConnectionOptions options, string profileName);

    /// <summary>
    /// Gets a list of stored credential profiles
    /// </summary>
    /// <returns>List of profile names</returns>
    IEnumerable<string> GetStoredProfiles();
}