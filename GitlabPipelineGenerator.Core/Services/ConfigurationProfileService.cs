using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for managing GitLab connection configuration profiles
/// </summary>
public class ConfigurationProfileService : IConfigurationProfileService
{
    private readonly ILogger<ConfigurationProfileService> _logger;
    private readonly ICredentialStorageService _credentialStorage;
    private readonly IGitLabAuthenticationService _authService;

    private const string ProfilePrefix = "GitLabPipelineGenerator_Config_";
    private const string DefaultProfileKey = "GitLabPipelineGenerator_DefaultProfile";

    public ConfigurationProfileService(
        ILogger<ConfigurationProfileService> logger,
        ICredentialStorageService credentialStorage,
        IGitLabAuthenticationService authService)
    {
        _logger = logger;
        _credentialStorage = credentialStorage;
        _authService = authService;
    }

    /// <inheritdoc />
    public async Task<bool> SaveProfileAsync(ConfigurationProfile profile)
    {
        try
        {
            var validationResult = await ValidateProfileAsync(profile);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Cannot save invalid profile '{ProfileName}'. Errors: {Errors}", 
                    profile.Name, string.Join(", ", validationResult.Errors));
                return false;
            }

            profile.Touch();

            var credentialData = new CredentialData
            {
                PersonalAccessToken = string.Empty, // Will be stored separately if needed
                InstanceUrl = profile.InstanceUrl,
                TimeoutSeconds = profile.TimeoutSeconds,
                MaxRetryAttempts = profile.MaxRetryAttempts,
                ProfileName = profile.Name,
                StoredAt = DateTime.UtcNow,
                Version = profile.Version
            };

            // Store profile configuration
            var target = $"{ProfilePrefix}{profile.Name}";
            var success = await _credentialStorage.StoreCredentialAsync(target, credentialData);

            if (success)
            {
                _logger.LogInformation("Successfully saved configuration profile: {ProfileName}", profile.Name);
                
                // If this is marked as default, set it as the default profile
                if (profile.IsDefault)
                {
                    await SetDefaultProfileAsync(profile.Name);
                }
            }
            else
            {
                _logger.LogError("Failed to save configuration profile: {ProfileName}", profile.Name);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration profile: {ProfileName}", profile.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile?> LoadProfileAsync(string profileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                _logger.LogWarning("Cannot load profile with empty name");
                return null;
            }

            var target = $"{ProfilePrefix}{profileName}";
            var credentialData = await _credentialStorage.LoadCredentialAsync(target);

            if (credentialData == null)
            {
                _logger.LogDebug("No configuration profile found: {ProfileName}", profileName);
                return null;
            }

            // Check if credentials are stored for this profile
            var storedCredentials = _authService.LoadStoredCredentials(profileName);
            var hasStoredCredentials = storedCredentials != null && 
                                     !string.IsNullOrWhiteSpace(storedCredentials.PersonalAccessToken);

            // Check if this is the default profile
            var defaultProfileName = await GetDefaultProfileNameAsync();
            var isDefault = string.Equals(profileName, defaultProfileName, StringComparison.OrdinalIgnoreCase);

            var profile = new ConfigurationProfile
            {
                Name = profileName,
                InstanceUrl = credentialData.InstanceUrl,
                TimeoutSeconds = credentialData.TimeoutSeconds,
                MaxRetryAttempts = credentialData.MaxRetryAttempts,
                HasStoredCredentials = hasStoredCredentials,
                IsDefault = isDefault,
                Version = credentialData.Version,
                CreatedAt = credentialData.StoredAt,
                LastModified = credentialData.StoredAt
            };

            _logger.LogDebug("Successfully loaded configuration profile: {ProfileName}", profileName);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration profile: {ProfileName}", profileName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProfileAsync(string profileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                _logger.LogWarning("Cannot delete profile with empty name");
                return false;
            }

            // Delete profile configuration
            var target = $"{ProfilePrefix}{profileName}";
            var configDeleted = await _credentialStorage.DeleteCredentialAsync(target);

            // Delete stored credentials if they exist
            var storedCredentials = _authService.LoadStoredCredentials(profileName);
            if (storedCredentials != null)
            {
                try
                {
                    _authService.StoreCredentials(new GitLabConnectionOptions
                    {
                        PersonalAccessToken = string.Empty,
                        ProfileName = profileName
                    }, profileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear stored credentials for profile: {ProfileName}", profileName);
                }
            }

            // Clear default profile if this was the default
            var defaultProfileName = await GetDefaultProfileNameAsync();
            if (string.Equals(profileName, defaultProfileName, StringComparison.OrdinalIgnoreCase))
            {
                await ClearDefaultProfileAsync();
            }

            if (configDeleted)
            {
                _logger.LogInformation("Successfully deleted configuration profile: {ProfileName}", profileName);
            }
            else
            {
                _logger.LogWarning("Failed to delete configuration profile: {ProfileName}", profileName);
            }

            return configDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration profile: {ProfileName}", profileName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListProfilesAsync()
    {
        try
        {
            var targets = await _credentialStorage.ListCredentialTargetsAsync(ProfilePrefix);
            
            var profiles = targets
                .Where(target => target.StartsWith(ProfilePrefix))
                .Select(target => target.Substring(ProfilePrefix.Length))
                .Where(profile => !string.IsNullOrWhiteSpace(profile))
                .OrderBy(profile => profile)
                .ToList();

            _logger.LogDebug("Found {Count} configuration profiles", profiles.Count);
            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing configuration profiles");
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile?> GetDefaultProfileAsync()
    {
        try
        {
            var defaultProfileName = await GetDefaultProfileNameAsync();
            
            if (string.IsNullOrWhiteSpace(defaultProfileName))
            {
                _logger.LogDebug("No default profile is set");
                return null;
            }

            return await LoadProfileAsync(defaultProfileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default configuration profile");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetDefaultProfileAsync(string profileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return await ClearDefaultProfileAsync();
            }

            // Verify the profile exists
            var profile = await LoadProfileAsync(profileName);
            if (profile == null)
            {
                _logger.LogWarning("Cannot set non-existent profile as default: {ProfileName}", profileName);
                return false;
            }

            // Store the default profile name
            var credentialData = new CredentialData
            {
                PersonalAccessToken = profileName, // Store profile name in token field
                InstanceUrl = "default-profile-marker",
                StoredAt = DateTime.UtcNow,
                Version = 1
            };

            var success = await _credentialStorage.StoreCredentialAsync(DefaultProfileKey, credentialData);

            if (success)
            {
                _logger.LogInformation("Set default configuration profile: {ProfileName}", profileName);
            }
            else
            {
                _logger.LogError("Failed to set default configuration profile: {ProfileName}", profileName);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default configuration profile: {ProfileName}", profileName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ProfileValidationResult> ValidateProfileAsync(ConfigurationProfile profile)
    {
        var result = new ProfileValidationResult { IsValid = true };

        try
        {
            // Validate profile name
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                result.AddError("Profile name is required");
            }
            else if (!IsValidProfileName(profile.Name))
            {
                result.AddError("Profile name contains invalid characters. Use only letters, numbers, hyphens, and underscores");
            }

            // Validate instance URL
            if (string.IsNullOrWhiteSpace(profile.InstanceUrl))
            {
                result.AddError("Instance URL is required");
            }
            else if (!Uri.TryCreate(profile.InstanceUrl, UriKind.Absolute, out var uri) || 
                     (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                result.AddError("Instance URL must be a valid HTTP or HTTPS URL");
            }

            // Validate timeout
            if (profile.TimeoutSeconds <= 0 || profile.TimeoutSeconds > 300)
            {
                result.AddError("Timeout must be between 1 and 300 seconds");
            }

            // Validate retry attempts
            if (profile.MaxRetryAttempts < 0 || profile.MaxRetryAttempts > 10)
            {
                result.AddError("Max retry attempts must be between 0 and 10");
            }

            // Validate version
            if (profile.Version <= 0)
            {
                result.AddError("Profile version must be greater than 0");
            }

            // Add warnings for common issues
            if (profile.InstanceUrl == "https://gitlab.com" && !string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                if (!profile.DisplayName.ToLowerInvariant().Contains("gitlab.com"))
                {
                    result.AddWarning("Profile appears to be for GitLab.com but display name doesn't indicate this");
                }
            }

            if (profile.TimeoutSeconds < 10)
            {
                result.AddWarning("Timeout is very low and may cause connection issues");
            }

            if (profile.MaxRetryAttempts == 0)
            {
                result.AddWarning("No retry attempts configured - API calls may fail on temporary network issues");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration profile: {ProfileName}", profile.Name);
            result.AddError($"Validation error: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ExportableProfile?> ExportProfileAsync(string profileName)
    {
        try
        {
            var profile = await LoadProfileAsync(profileName);
            if (profile == null)
            {
                _logger.LogWarning("Cannot export non-existent profile: {ProfileName}", profileName);
                return null;
            }

            var exportableProfile = ExportableProfile.FromConfigurationProfile(profile);
            
            _logger.LogInformation("Exported configuration profile: {ProfileName}", profileName);
            return exportableProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration profile: {ProfileName}", profileName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ImportProfileAsync(ExportableProfile exportableProfile, string profileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                _logger.LogWarning("Cannot import profile with empty name");
                return false;
            }

            var profile = exportableProfile.ToConfigurationProfile(profileName);
            
            var validationResult = await ValidateProfileAsync(profile);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Cannot import invalid profile '{ProfileName}'. Errors: {Errors}", 
                    profileName, string.Join(", ", validationResult.Errors));
                return false;
            }

            var success = await SaveProfileAsync(profile);
            
            if (success)
            {
                _logger.LogInformation("Successfully imported configuration profile: {ProfileName}", profileName);
            }
            else
            {
                _logger.LogError("Failed to import configuration profile: {ProfileName}", profileName);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration profile: {ProfileName}", profileName);
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Gets the name of the default profile
    /// </summary>
    private async Task<string?> GetDefaultProfileNameAsync()
    {
        try
        {
            var credentialData = await _credentialStorage.LoadCredentialAsync(DefaultProfileKey);
            
            if (credentialData?.PersonalAccessToken != null && 
                credentialData.InstanceUrl == "default-profile-marker")
            {
                return credentialData.PersonalAccessToken;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting default profile name");
            return null;
        }
    }

    /// <summary>
    /// Clears the default profile setting
    /// </summary>
    private async Task<bool> ClearDefaultProfileAsync()
    {
        try
        {
            var success = await _credentialStorage.DeleteCredentialAsync(DefaultProfileKey);
            
            if (success)
            {
                _logger.LogInformation("Cleared default configuration profile");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing default configuration profile");
            return false;
        }
    }

    /// <summary>
    /// Validates that a profile name contains only allowed characters
    /// </summary>
    private static bool IsValidProfileName(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return false;

        // Allow letters, numbers, hyphens, underscores, and dots
        var regex = new Regex(@"^[a-zA-Z0-9\-_.]+$");
        return regex.IsMatch(profileName) && profileName.Length <= 50;
    }

    #endregion
}