using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Schema;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for configuration validation, migration, and settings management
/// </summary>
public class ConfigurationManagementService : IConfigurationManagementService
{
    private readonly ILogger<ConfigurationManagementService> _logger;
    private readonly ICredentialStorageService _credentialStorage;
    private readonly IConfigurationProfileService _profileService;

    private const string SettingsKey = "GitLabPipelineGenerator_Settings";
    private const string VersionKey = "GitLabPipelineGenerator_Version";
    private const int CurrentVersion = 1;

    public ConfigurationManagementService(
        ILogger<ConfigurationManagementService> logger,
        ICredentialStorageService credentialStorage,
        IConfigurationProfileService profileService)
    {
        _logger = logger;
        _credentialStorage = credentialStorage;
        _profileService = profileService;
    }

    /// <inheritdoc />
    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync()
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        try
        {
            _logger.LogDebug("Starting configuration validation");

            // Validate settings
            var settings = await GetSettingsAsync();
            await ValidateSettings(settings, result);
            result.ValidatedItems.Add("Settings");

            // Validate profiles
            var profiles = await _profileService.ListProfilesAsync();
            foreach (var profileName in profiles)
            {
                var profile = await _profileService.LoadProfileAsync(profileName);
                if (profile != null)
                {
                    var profileValidation = await _profileService.ValidateProfileAsync(profile);
                    if (!profileValidation.IsValid)
                    {
                        result.Errors.AddRange(profileValidation.Errors.Select(e => $"Profile '{profileName}': {e}"));
                        result.IsValid = false;
                    }
                    result.Warnings.AddRange(profileValidation.Warnings.Select(w => $"Profile '{profileName}': {w}"));
                }
                result.ValidatedItems.Add($"Profile: {profileName}");
            }

            // Validate credential storage
            if (!_credentialStorage.IsAvailable)
            {
                result.Warnings.Add("Credential storage is not available on this platform");
            }
            result.ValidatedItems.Add("Credential Storage");

            // Validate version
            var currentVersion = await GetConfigurationVersionAsync();
            if (currentVersion < CurrentVersion)
            {
                result.Warnings.Add($"Configuration version ({currentVersion}) is older than current version ({CurrentVersion}). Consider migrating.");
            }
            else if (currentVersion > CurrentVersion)
            {
                result.Warnings.Add($"Configuration version ({currentVersion}) is newer than supported version ({CurrentVersion})");
            }
            result.ValidatedItems.Add("Version");

            _logger.LogInformation("Configuration validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration validation");
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationMigrationResult> MigrateConfigurationAsync()
    {
        var result = new ConfigurationMigrationResult();

        try
        {
            var currentVersion = await GetConfigurationVersionAsync();
            result.FromVersion = currentVersion;
            result.ToVersion = CurrentVersion;

            _logger.LogInformation("Starting configuration migration from version {FromVersion} to {ToVersion}",
                currentVersion, CurrentVersion);

            if (currentVersion == CurrentVersion)
            {
                result.Success = true;
                result.MigrationSteps.Add("No migration needed - already at current version");
                _logger.LogInformation("No migration needed - configuration is already at current version");
                return result;
            }

            // Create backup before migration
            try
            {
                var backup = await ExportConfigurationAsync();
                if (backup != null)
                {
                    var backupPath = Path.Combine(Path.GetTempPath(), $"gitlab-config-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
                    var backupJson = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(backupPath, backupJson);
                    
                    result.BackupCreated = true;
                    result.BackupPath = backupPath;
                    result.MigrationSteps.Add($"Created backup at: {backupPath}");
                    _logger.LogInformation("Created configuration backup at: {BackupPath}", backupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create configuration backup");
                result.Warnings.Add("Failed to create backup before migration");
            }

            // Perform version-specific migrations
            if (currentVersion < 1)
            {
                await MigrateToVersion1(result);
            }

            // Update version
            var versionUpdated = await SetConfigurationVersionAsync(CurrentVersion);
            if (versionUpdated)
            {
                result.MigrationSteps.Add($"Updated configuration version to {CurrentVersion}");
                result.Success = true;
                _logger.LogInformation("Configuration migration completed successfully");
            }
            else
            {
                result.Errors.Add("Failed to update configuration version");
                result.Success = false;
                _logger.LogError("Failed to update configuration version after migration");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration migration");
            result.Success = false;
            result.Errors.Add($"Migration error: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetConfigurationVersionAsync()
    {
        try
        {
            var credentialData = await _credentialStorage.LoadCredentialAsync(VersionKey);
            
            if (credentialData?.PersonalAccessToken != null && 
                int.TryParse(credentialData.PersonalAccessToken, out var version))
            {
                return version;
            }

            // Default to version 0 if not found
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting configuration version, defaulting to 0");
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetConfigurationVersionAsync(int version)
    {
        try
        {
            var credentialData = new CredentialData
            {
                PersonalAccessToken = version.ToString(),
                InstanceUrl = "version-marker",
                StoredAt = DateTime.UtcNow,
                Version = 1
            };

            var success = await _credentialStorage.StoreCredentialAsync(VersionKey, credentialData);
            
            if (success)
            {
                _logger.LogDebug("Set configuration version to: {Version}", version);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration version: {Version}", version);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationSettings> GetSettingsAsync()
    {
        try
        {
            var credentialData = await _credentialStorage.LoadCredentialAsync(SettingsKey);
            
            if (credentialData?.PersonalAccessToken != null)
            {
                var settings = JsonSerializer.Deserialize<ConfigurationSettings>(credentialData.PersonalAccessToken);
                if (settings != null)
                {
                    _logger.LogDebug("Loaded configuration settings");
                    return settings;
                }
            }

            // Return default settings if not found
            var defaultSettings = new ConfigurationSettings();
            _logger.LogDebug("Using default configuration settings");
            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration settings, using defaults");
            return new ConfigurationSettings();
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateSettingsAsync(ConfigurationSettings settings)
    {
        try
        {
            settings.LastUpdated = DateTime.UtcNow;
            settings.Version = CurrentVersion;

            var json = JsonSerializer.Serialize(settings);
            var credentialData = new CredentialData
            {
                PersonalAccessToken = json,
                InstanceUrl = "settings-marker",
                StoredAt = DateTime.UtcNow,
                Version = 1
            };

            var success = await _credentialStorage.StoreCredentialAsync(SettingsKey, credentialData);
            
            if (success)
            {
                _logger.LogInformation("Updated configuration settings");
            }
            else
            {
                _logger.LogError("Failed to update configuration settings");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration settings");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResetConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Resetting configuration to defaults");

            // Reset settings
            var defaultSettings = new ConfigurationSettings();
            var settingsReset = await UpdateSettingsAsync(defaultSettings);

            // Clear all profiles
            var profiles = await _profileService.ListProfilesAsync();
            var profilesCleared = 0;
            
            foreach (var profileName in profiles)
            {
                var deleted = await _profileService.DeleteProfileAsync(profileName);
                if (deleted)
                {
                    profilesCleared++;
                }
            }

            // Reset version
            var versionReset = await SetConfigurationVersionAsync(CurrentVersion);

            var success = settingsReset && versionReset;
            
            if (success)
            {
                _logger.LogInformation("Configuration reset completed. Cleared {ProfileCount} profiles", profilesCleared);
            }
            else
            {
                _logger.LogError("Configuration reset failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ExportableConfiguration?> ExportConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Exporting configuration");

            var exportConfig = new ExportableConfiguration
            {
                Settings = await GetSettingsAsync(),
                Metadata = new ExportMetadata
                {
                    ExportedAt = DateTime.UtcNow,
                    Version = CurrentVersion,
                    ApplicationVersion = GetType().Assembly.GetName().Version?.ToString(),
                    Description = "GitLab Pipeline Generator Configuration Export"
                }
            };

            // Export profiles
            var profiles = await _profileService.ListProfilesAsync();
            foreach (var profileName in profiles)
            {
                var exportableProfile = await _profileService.ExportProfileAsync(profileName);
                if (exportableProfile != null)
                {
                    exportConfig.Profiles.Add(exportableProfile);
                }
            }

            _logger.LogInformation("Exported configuration with {ProfileCount} profiles", exportConfig.Profiles.Count);
            return exportConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationImportResult> ImportConfigurationAsync(ExportableConfiguration configuration, bool overwriteExisting = false)
    {
        var result = new ConfigurationImportResult();

        try
        {
            _logger.LogInformation("Importing configuration with {ProfileCount} profiles. Overwrite: {Overwrite}",
                configuration.Profiles.Count, overwriteExisting);

            // Import settings
            if (configuration.Settings != null)
            {
                var settingsImported = await UpdateSettingsAsync(configuration.Settings);
                if (settingsImported)
                {
                    result.SettingsImported = true;
                    result.ImportedItems.Add("Settings");
                }
                else
                {
                    result.Errors.Add("Failed to import settings");
                }
            }

            // Import profiles
            var existingProfiles = (await _profileService.ListProfilesAsync()).ToHashSet();
            
            foreach (var exportableProfile in configuration.Profiles)
            {
                // Generate a unique profile name
                var profileName = GenerateUniqueProfileName(exportableProfile.DisplayName ?? "imported-profile", existingProfiles);
                
                if (existingProfiles.Contains(profileName) && !overwriteExisting)
                {
                    result.ProfilesSkipped++;
                    result.Warnings.Add($"Skipped existing profile: {profileName}");
                    continue;
                }

                var imported = await _profileService.ImportProfileAsync(exportableProfile, profileName);
                if (imported)
                {
                    result.ProfilesImported++;
                    result.ImportedItems.Add($"Profile: {profileName}");
                    existingProfiles.Add(profileName);
                }
                else
                {
                    result.Errors.Add($"Failed to import profile: {profileName}");
                }
            }

            result.Success = result.Errors.Count == 0;
            
            _logger.LogInformation("Configuration import completed. Success: {Success}, Profiles imported: {ProfilesImported}, Profiles skipped: {ProfilesSkipped}",
                result.Success, result.ProfilesImported, result.ProfilesSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration");
            result.Success = false;
            result.Errors.Add($"Import error: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<SchemaValidationResult> ValidateSchemaAsync(string configurationJson)
    {
        var result = new SchemaValidationResult { ExpectedVersion = CurrentVersion };

        try
        {
            // Basic JSON validation
            var jsonDocument = JsonDocument.Parse(configurationJson);
            
            // Try to detect version
            if (jsonDocument.RootElement.TryGetProperty("metadata", out var metadata) &&
                metadata.TryGetProperty("version", out var versionElement))
            {
                result.DetectedVersion = versionElement.GetInt32();
            }

            // Validate structure
            var requiredProperties = new[] { "settings", "profiles", "metadata" };
            foreach (var property in requiredProperties)
            {
                if (!jsonDocument.RootElement.TryGetProperty(property, out _))
                {
                    result.Errors.Add($"Missing required property: {property}");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            
            _logger.LogDebug("Schema validation completed. Valid: {IsValid}, Detected version: {DetectedVersion}",
                result.IsValid, result.DetectedVersion);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error during schema validation");
            result.IsValid = false;
            result.Errors.Add($"Invalid JSON: {ex.Message}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during schema validation");
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationHealth> GetHealthStatusAsync()
    {
        var health = new ConfigurationHealth
        {
            ConfigurationVersion = await GetConfigurationVersionAsync(),
            LatestVersion = CurrentVersion,
            CredentialStorageAvailable = _credentialStorage.IsAvailable,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            // Check profiles
            var profiles = await _profileService.ListProfilesAsync();
            health.ProfileCount = profiles.Count();

            var profilesWithCredentials = 0;
            foreach (var profileName in profiles)
            {
                var profile = await _profileService.LoadProfileAsync(profileName);
                if (profile?.HasStoredCredentials == true)
                {
                    profilesWithCredentials++;
                }
            }
            health.ProfilesWithCredentials = profilesWithCredentials;

            // Check default profile
            var defaultProfile = await _profileService.GetDefaultProfileAsync();
            health.HasDefaultProfile = defaultProfile != null;

            // Determine overall status
            if (health.ConfigurationVersion < health.LatestVersion)
            {
                health.Status = HealthStatus.Warning;
                health.Issues.Add($"Configuration version ({health.ConfigurationVersion}) is outdated");
                health.Recommendations.Add("Run configuration migration to update to the latest version");
            }

            if (!health.CredentialStorageAvailable)
            {
                health.Status = HealthStatus.Warning;
                health.Issues.Add("Credential storage is not available on this platform");
                health.Recommendations.Add("Credentials cannot be stored securely");
            }

            if (health.ProfileCount == 0)
            {
                health.Status = HealthStatus.Warning;
                health.Issues.Add("No configuration profiles found");
                health.Recommendations.Add("Create at least one configuration profile");
            }

            if (!health.HasDefaultProfile && health.ProfileCount > 0)
            {
                health.Issues.Add("No default profile is set");
                health.Recommendations.Add("Set a default profile for easier usage");
            }

            // If no issues found, status is healthy
            if (health.Status == HealthStatus.Unknown && health.Issues.Count == 0)
            {
                health.Status = HealthStatus.Healthy;
            }

            _logger.LogDebug("Configuration health check completed. Status: {Status}, Issues: {IssueCount}",
                health.Status, health.Issues.Count);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration health check");
            health.Status = HealthStatus.Error;
            health.Issues.Add($"Health check error: {ex.Message}");
            return health;
        }
    }

    #region Private Methods

    /// <summary>
    /// Validates configuration settings
    /// </summary>
    private async Task ValidateSettings(ConfigurationSettings settings, ConfigurationValidationResult result)
    {
        if (settings.DefaultTimeoutSeconds <= 0 || settings.DefaultTimeoutSeconds > 300)
        {
            result.Errors.Add("Default timeout must be between 1 and 300 seconds");
            result.IsValid = false;
        }

        if (settings.DefaultMaxRetryAttempts < 0 || settings.DefaultMaxRetryAttempts > 10)
        {
            result.Errors.Add("Default max retry attempts must be between 0 and 10");
            result.IsValid = false;
        }

        if (settings.Cache.ExpirationMinutes <= 0)
        {
            result.Warnings.Add("Cache expiration is set to 0 or negative value");
        }

        if (settings.Cache.MaxSizeMB <= 0)
        {
            result.Warnings.Add("Cache max size is set to 0 or negative value");
        }

        await Task.CompletedTask; // Make method async for future enhancements
    }

    /// <summary>
    /// Performs migration to version 1
    /// </summary>
    private async Task MigrateToVersion1(ConfigurationMigrationResult result)
    {
        // Version 1 migration steps would go here
        // For now, just ensure default settings exist
        var settings = await GetSettingsAsync();
        var updated = await UpdateSettingsAsync(settings);
        
        if (updated)
        {
            result.MigrationSteps.Add("Initialized default configuration settings");
        }
        else
        {
            result.Errors.Add("Failed to initialize default configuration settings");
        }
    }

    /// <summary>
    /// Generates a unique profile name
    /// </summary>
    private static string GenerateUniqueProfileName(string baseName, HashSet<string> existingNames)
    {
        var cleanName = baseName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        if (!existingNames.Contains(cleanName))
        {
            return cleanName;
        }

        var counter = 1;
        string uniqueName;
        do
        {
            uniqueName = $"{cleanName}-{counter}";
            counter++;
        } while (existingNames.Contains(uniqueName));

        return uniqueName;
    }

    #endregion
}