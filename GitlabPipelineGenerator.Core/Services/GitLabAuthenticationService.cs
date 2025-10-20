using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.GitLabApiClient;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text.Json;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for handling GitLab API authentication and credential management
/// </summary>
public class GitLabAuthenticationService : IGitLabAuthenticationService
{
    private readonly ILogger<GitLabAuthenticationService> _logger;
    private readonly ResilientGitLabService _resilientService;
    private readonly ICredentialStorageService _credentialStorage;
    private GitLabClient? _currentClient;
    private GitLabConnectionOptions? _currentOptions;

    private const string CredentialTarget = "GitLabPipelineGenerator";
    private const string ProfilePrefix = "GitLabPipelineGenerator_Profile_";

    public GitLabAuthenticationService(
        ILogger<GitLabAuthenticationService> logger,
        IGitLabApiErrorHandler errorHandler,
        ICredentialStorageService credentialStorage)
    {
        _logger = logger;
        _resilientService = new ResilientGitLabService(errorHandler);
        _credentialStorage = credentialStorage;
    }

    /// <inheritdoc />
    public async Task<GitLabClient> AuthenticateAsync(GitLabConnectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PersonalAccessToken))
        {
            throw new ArgumentException("Personal access token is required", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.InstanceUrl))
        {
            throw new ArgumentException("Instance URL is required", nameof(options));
        }

        return await _resilientService.ExecuteAsync(async cancellationToken =>
        {
            _logger.LogInformation("Authenticating with GitLab instance: {InstanceUrl}", options.InstanceUrl);

            var client = new GitLabClient(options.InstanceUrl, options.PersonalAccessToken);

            // Test the connection by making a simple API call with resilience
            await TestConnectionAsync(client, cancellationToken);

            _logger.LogInformation("Successfully authenticated with GitLab instance");

            _currentClient = client;
            _currentOptions = options;

            // Store credentials if requested
            if (options.StoreCredentials)
            {
                if (!string.IsNullOrWhiteSpace(options.ProfileName))
                {
                    StoreCredentials(options, options.ProfileName);
                }
                else
                {
                    StoreCredentials(options);
                }
            }

            return client;
        }, RetryPolicy.Conservative, TimeSpan.FromSeconds(options.TimeoutSeconds));
    }

    /// <inheritdoc />
    public async Task<bool> ValidateTokenAsync(string token, string? instanceUrl = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var url = instanceUrl ?? "https://gitlab.com";

        try
        {
            return await _resilientService.ExecuteAsync(async cancellationToken =>
            {
                var client = new GitLabClient(url, token);
                await TestConnectionAsync(client, cancellationToken);
                return true;
            }, RetryPolicy.Conservative, TimeSpan.FromSeconds(15));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed for instance: {InstanceUrl}", url);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<GitLabUserInfo> GetCurrentUserAsync()
    {
        if (_currentClient == null)
        {
            throw new InvalidOperationException("No authenticated GitLab client available. Call AuthenticateAsync first.");
        }

        try
        {
            // TODO: Implement proper user info retrieval once API methods are verified
            // For now, return a placeholder
            var userInfo = new GitLabUserInfo
            {
                Id = 0,
                Username = "Username",
                Name = "Authenticated User",
                Email = "user@example.com",
                State = "active"
            };

            return Task.FromResult(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user information");
            throw new InvalidOperationException($"Failed to get user information: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public void StoreCredentials(GitLabConnectionOptions options)
    {
        StoreCredentials(options, "default");
    }

    /// <inheritdoc />
    public void StoreCredentials(GitLabConnectionOptions options, string profileName)
    {
        if (string.IsNullOrWhiteSpace(options.PersonalAccessToken))
        {
            throw new ArgumentException("Personal access token is required to store credentials", nameof(options));
        }

        try
        {
            var credentialData = CredentialData.FromConnectionOptions(options);
            credentialData.ProfileName = profileName == "default" ? null : profileName;

            var target = profileName == "default" ? CredentialTarget : $"{ProfilePrefix}{profileName}";

            var success = _credentialStorage.StoreCredentialAsync(target, credentialData).GetAwaiter().GetResult();

            if (success)
            {
                _logger.LogInformation("Credentials stored successfully for profile: {ProfileName}", profileName);
            }
            else
            {
                _logger.LogWarning("Failed to store credentials for profile: {ProfileName}. Credential storage may not be available.", profileName);
                throw new InvalidOperationException("Failed to store credentials. Credential storage may not be available on this platform.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store credentials for profile: {ProfileName}", profileName);
            throw new InvalidOperationException($"Failed to store credentials: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public GitLabConnectionOptions? LoadStoredCredentials()
    {
        return LoadStoredCredentials("default");
    }

    /// <inheritdoc />
    public GitLabConnectionOptions? LoadStoredCredentials(string profileName)
    {
        try
        {
            var target = profileName == "default" ? CredentialTarget : $"{ProfilePrefix}{profileName}";
            var credentialData = _credentialStorage.LoadCredentialAsync(target).GetAwaiter().GetResult();

            if (credentialData == null)
            {
                _logger.LogDebug("No stored credentials found for profile: {ProfileName}", profileName);
                return null;
            }

            var connectionOptions = credentialData.ToConnectionOptions();
            connectionOptions.ProfileName = profileName == "default" ? null : profileName;

            _logger.LogDebug("Successfully loaded stored credentials for profile: {ProfileName}", profileName);
            return connectionOptions;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load stored credentials for profile: {ProfileName}", profileName);
            return null;
        }
    }

    /// <inheritdoc />
    public void ClearStoredCredentials()
    {
        try
        {
            // Clear default profile
            var defaultCleared = _credentialStorage.DeleteCredentialAsync(CredentialTarget).GetAwaiter().GetResult();

            // Clear all named profiles
            var profiles = GetStoredProfiles().ToList();
            var profilesCleared = 0;

            foreach (var profile in profiles)
            {
                var success = _credentialStorage.DeleteCredentialAsync($"{ProfilePrefix}{profile}").GetAwaiter().GetResult();
                if (success)
                {
                    profilesCleared++;
                }
            }

            _logger.LogInformation("Cleared {ProfileCount} stored credential profiles. Default profile cleared: {DefaultCleared}",
                profilesCleared, defaultCleared);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear stored credentials");
            throw new InvalidOperationException($"Failed to clear credentials: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetStoredProfiles()
    {
        try
        {
            var targets = _credentialStorage.ListCredentialTargetsAsync(ProfilePrefix).GetAwaiter().GetResult();

            // Extract profile names from targets by removing the prefix
            var profiles = targets
                .Where(target => target.StartsWith(ProfilePrefix))
                .Select(target => target.Substring(ProfilePrefix.Length))
                .Where(profile => !string.IsNullOrWhiteSpace(profile))
                .ToList();

            _logger.LogDebug("Found {Count} stored credential profiles", profiles.Count);
            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate stored credential profiles");
            return Enumerable.Empty<string>();
        }
    }

    #region Private Methods

    /// <summary>
    /// Tests the GitLab connection with a simple API call
    /// </summary>
    private async Task TestConnectionAsync(GitLabClient client, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get projects as a basic connectivity test
            var projects = await client.GetProjectsAsync(owned: true, perPage: 1);

            // If we get here without exception, the connection works
            _logger.LogDebug("Connection test successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            throw new InvalidOperationException("Failed to connect to GitLab API", ex);
        }
    }

    #endregion
}