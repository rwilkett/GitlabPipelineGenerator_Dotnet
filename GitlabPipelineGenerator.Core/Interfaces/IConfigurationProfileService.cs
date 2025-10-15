using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service for managing GitLab connection profiles
/// </summary>
public interface IConfigurationProfileService
{
    /// <summary>
    /// Creates or updates a configuration profile
    /// </summary>
    /// <param name="profile">Configuration profile to save</param>
    /// <returns>True if the profile was saved successfully</returns>
    Task<bool> SaveProfileAsync(ConfigurationProfile profile);

    /// <summary>
    /// Loads a configuration profile by name
    /// </summary>
    /// <param name="profileName">Name of the profile to load</param>
    /// <returns>Configuration profile, or null if not found</returns>
    Task<ConfigurationProfile?> LoadProfileAsync(string profileName);

    /// <summary>
    /// Deletes a configuration profile
    /// </summary>
    /// <param name="profileName">Name of the profile to delete</param>
    /// <returns>True if the profile was deleted successfully</returns>
    Task<bool> DeleteProfileAsync(string profileName);

    /// <summary>
    /// Lists all available configuration profiles
    /// </summary>
    /// <returns>List of configuration profile names</returns>
    Task<IEnumerable<string>> ListProfilesAsync();

    /// <summary>
    /// Gets the default configuration profile
    /// </summary>
    /// <returns>Default configuration profile, or null if not set</returns>
    Task<ConfigurationProfile?> GetDefaultProfileAsync();

    /// <summary>
    /// Sets the default configuration profile
    /// </summary>
    /// <param name="profileName">Name of the profile to set as default</param>
    /// <returns>True if the default profile was set successfully</returns>
    Task<bool> SetDefaultProfileAsync(string profileName);

    /// <summary>
    /// Validates a configuration profile
    /// </summary>
    /// <param name="profile">Configuration profile to validate</param>
    /// <returns>Validation result with any errors</returns>
    Task<ProfileValidationResult> ValidateProfileAsync(ConfigurationProfile profile);

    /// <summary>
    /// Exports a configuration profile (without sensitive data)
    /// </summary>
    /// <param name="profileName">Name of the profile to export</param>
    /// <returns>Exportable profile data, or null if profile not found</returns>
    Task<ExportableProfile?> ExportProfileAsync(string profileName);

    /// <summary>
    /// Imports a configuration profile from exportable data
    /// </summary>
    /// <param name="exportableProfile">Exportable profile data</param>
    /// <param name="profileName">Name for the imported profile</param>
    /// <returns>True if the profile was imported successfully</returns>
    Task<bool> ImportProfileAsync(ExportableProfile exportableProfile, string profileName);
}