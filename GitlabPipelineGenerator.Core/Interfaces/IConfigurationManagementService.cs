using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service for configuration validation, migration, and settings management
/// </summary>
public interface IConfigurationManagementService
{
    /// <summary>
    /// Validates the overall configuration state
    /// </summary>
    /// <returns>Configuration validation result</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync();

    /// <summary>
    /// Migrates configuration to the latest version
    /// </summary>
    /// <returns>Migration result</returns>
    Task<ConfigurationMigrationResult> MigrateConfigurationAsync();

    /// <summary>
    /// Gets the current configuration version
    /// </summary>
    /// <returns>Current configuration version</returns>
    Task<int> GetConfigurationVersionAsync();

    /// <summary>
    /// Sets the configuration version
    /// </summary>
    /// <param name="version">Version to set</param>
    /// <returns>True if version was set successfully</returns>
    Task<bool> SetConfigurationVersionAsync(int version);

    /// <summary>
    /// Gets all configuration settings
    /// </summary>
    /// <returns>Configuration settings</returns>
    Task<ConfigurationSettings> GetSettingsAsync();

    /// <summary>
    /// Updates configuration settings
    /// </summary>
    /// <param name="settings">Settings to update</param>
    /// <returns>True if settings were updated successfully</returns>
    Task<bool> UpdateSettingsAsync(ConfigurationSettings settings);

    /// <summary>
    /// Resets configuration to default values
    /// </summary>
    /// <returns>True if configuration was reset successfully</returns>
    Task<bool> ResetConfigurationAsync();

    /// <summary>
    /// Exports the entire configuration
    /// </summary>
    /// <returns>Exportable configuration data</returns>
    Task<ExportableConfiguration?> ExportConfigurationAsync();

    /// <summary>
    /// Imports configuration from exportable data
    /// </summary>
    /// <param name="configuration">Configuration data to import</param>
    /// <param name="overwriteExisting">Whether to overwrite existing configuration</param>
    /// <returns>Import result</returns>
    Task<ConfigurationImportResult> ImportConfigurationAsync(ExportableConfiguration configuration, bool overwriteExisting = false);

    /// <summary>
    /// Validates configuration file schema
    /// </summary>
    /// <param name="configurationJson">Configuration JSON to validate</param>
    /// <returns>Schema validation result</returns>
    Task<SchemaValidationResult> ValidateSchemaAsync(string configurationJson);

    /// <summary>
    /// Gets configuration health status
    /// </summary>
    /// <returns>Configuration health information</returns>
    Task<ConfigurationHealth> GetHealthStatusAsync();
}