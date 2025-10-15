using FluentAssertions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationManagementService
/// </summary>
public class ConfigurationManagementServiceTests
{
    private readonly Mock<ILogger<ConfigurationManagementService>> _mockLogger;
    private readonly Mock<ICredentialStorageService> _mockCredentialStorage;
    private readonly Mock<IConfigurationProfileService> _mockProfileService;
    private readonly ConfigurationManagementService _configService;

    public ConfigurationManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationManagementService>>();
        _mockCredentialStorage = new Mock<ICredentialStorageService>();
        _mockProfileService = new Mock<IConfigurationProfileService>();
        
        _configService = new ConfigurationManagementService(
            _mockLogger.Object,
            _mockCredentialStorage.Object,
            _mockProfileService.Object);
    }

    #region ValidateConfigurationAsync Tests

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfiguration_ShouldReturnSuccess()
    {
        // Arrange
        var validSettings = new ConfigurationSettings
        {
            DefaultTimeoutSeconds = 30,
            DefaultMaxRetryAttempts = 3,
            Cache = new CacheSettings { ExpirationMinutes = 60, MaxSizeMB = 100 }
        };

        var validProfile = new ConfigurationProfile
        {
            Name = "test-profile",
            InstanceUrl = "https://gitlab.com"
        };

        var profileValidation = ProfileValidationResult.Success();

        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(validSettings),
                InstanceUrl = "settings-marker"
            });
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "test-profile" });
        _mockProfileService.Setup(x => x.LoadProfileAsync("test-profile"))
            .ReturnsAsync(validProfile);
        _mockProfileService.Setup(x => x.ValidateProfileAsync(validProfile))
            .ReturnsAsync(profileValidation);

        // Act
        var result = await _configService.ValidateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.ValidatedItems.Should().Contain("Settings");
        result.ValidatedItems.Should().Contain("Profile: test-profile");
        result.ValidatedItems.Should().Contain("Credential Storage");
        result.ValidatedItems.Should().Contain("Version");
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidSettings_ShouldReturnFailure()
    {
        // Arrange
        var invalidSettings = new ConfigurationSettings
        {
            DefaultTimeoutSeconds = -1, // Invalid
            DefaultMaxRetryAttempts = 15, // Invalid
            Cache = new CacheSettings { ExpirationMinutes = -5, MaxSizeMB = -10 }
        };

        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(invalidSettings),
                InstanceUrl = "settings-marker"
            });
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _configService.ValidateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Default timeout must be between 1 and 300 seconds");
        result.Errors.Should().Contain("Default max retry attempts must be between 0 and 10");
        result.Warnings.Should().Contain("Cache expiration is set to 0 or negative value");
        result.Warnings.Should().Contain("Cache max size is set to 0 or negative value");
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidProfile_ShouldReturnFailure()
    {
        // Arrange
        var validSettings = new ConfigurationSettings();
        var invalidProfile = new ConfigurationProfile
        {
            Name = "test-profile",
            InstanceUrl = "invalid-url"
        };

        var profileValidation = ProfileValidationResult.Failure("Invalid instance URL");

        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(validSettings),
                InstanceUrl = "settings-marker"
            });
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "test-profile" });
        _mockProfileService.Setup(x => x.LoadProfileAsync("test-profile"))
            .ReturnsAsync(invalidProfile);
        _mockProfileService.Setup(x => x.ValidateProfileAsync(invalidProfile))
            .ReturnsAsync(profileValidation);

        // Act
        var result = await _configService.ValidateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Profile 'test-profile': Invalid instance URL");
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithUnavailableCredentialStorage_ShouldAddWarning()
    {
        // Arrange
        var validSettings = new ConfigurationSettings();

        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(false);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(validSettings),
                InstanceUrl = "settings-marker"
            });
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _configService.ValidateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("Credential storage is not available on this platform");
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithException_ShouldReturnFailure()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        var result = await _configService.ValidateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Validation error: Storage error");
    }

    #endregion

    #region MigrateConfigurationAsync Tests

    [Fact]
    public async Task MigrateConfigurationAsync_WithCurrentVersion_ShouldReturnNoMigrationNeeded()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        // Act
        var result = await _configService.MigrateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FromVersion.Should().Be(1);
        result.ToVersion.Should().Be(1);
        result.MigrationSteps.Should().Contain("No migration needed - already at current version");
    }

    [Fact]
    public async Task MigrateConfigurationAsync_FromVersion0_ShouldMigrateToVersion1()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync((CredentialData?)null); // No version stored = version 0

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync((CredentialData?)null); // No settings stored

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Version", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _configService.MigrateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FromVersion.Should().Be(0);
        result.ToVersion.Should().Be(1);
        result.MigrationSteps.Should().Contain("Initialized default configuration settings");
        result.MigrationSteps.Should().Contain("Updated configuration version to 1");
    }

    [Fact]
    public async Task MigrateConfigurationAsync_WithBackupCreation_ShouldCreateBackup()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync((CredentialData?)null);

        var settings = new ConfigurationSettings();
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(settings),
                InstanceUrl = "settings-marker"
            });

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _configService.MigrateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupCreated.Should().BeTrue();
        result.BackupPath.Should().NotBeNullOrEmpty();
        result.MigrationSteps.Should().Contain(step => step.StartsWith("Created backup at:"));
    }

    [Fact]
    public async Task MigrateConfigurationAsync_WithStorageFailure_ShouldReturnFailure()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync((CredentialData?)null);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync((CredentialData?)null);

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Version", It.IsAny<CredentialData>()))
            .ReturnsAsync(false); // Fail version update

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _configService.MigrateConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Failed to update configuration version");
    }

    #endregion

    #region Configuration Version Tests

    [Fact]
    public async Task GetConfigurationVersionAsync_WithStoredVersion_ShouldReturnVersion()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "2",
                InstanceUrl = "version-marker"
            });

        // Act
        var version = await _configService.GetConfigurationVersionAsync();

        // Assert
        version.Should().Be(2);
    }

    [Fact]
    public async Task GetConfigurationVersionAsync_WithNoStoredVersion_ShouldReturnZero()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var version = await _configService.GetConfigurationVersionAsync();

        // Assert
        version.Should().Be(0);
    }

    [Fact]
    public async Task GetConfigurationVersionAsync_WithInvalidVersion_ShouldReturnZero()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "invalid",
                InstanceUrl = "version-marker"
            });

        // Act
        var version = await _configService.GetConfigurationVersionAsync();

        // Assert
        version.Should().Be(0);
    }

    [Fact]
    public async Task SetConfigurationVersionAsync_WithValidVersion_ShouldReturnTrue()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Version", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        var result = await _configService.SetConfigurationVersionAsync(3);

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Version", 
            It.Is<CredentialData>(c => c.PersonalAccessToken == "3")), Times.Once);
    }

    [Fact]
    public async Task SetConfigurationVersionAsync_WithStorageFailure_ShouldReturnFalse()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Version", It.IsAny<CredentialData>()))
            .ReturnsAsync(false);

        // Act
        var result = await _configService.SetConfigurationVersionAsync(3);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Settings Management Tests

    [Fact]
    public async Task GetSettingsAsync_WithStoredSettings_ShouldReturnSettings()
    {
        // Arrange
        var settings = new ConfigurationSettings
        {
            DefaultTimeoutSeconds = 45,
            DefaultMaxRetryAttempts = 5
        };

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(settings),
                InstanceUrl = "settings-marker"
            });

        // Act
        var result = await _configService.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.DefaultTimeoutSeconds.Should().Be(45);
        result.DefaultMaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public async Task GetSettingsAsync_WithNoStoredSettings_ShouldReturnDefaults()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _configService.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.DefaultTimeoutSeconds.Should().Be(30); // Default value
        result.DefaultMaxRetryAttempts.Should().Be(3); // Default value
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithValidSettings_ShouldReturnTrue()
    {
        // Arrange
        var settings = new ConfigurationSettings
        {
            DefaultTimeoutSeconds = 60,
            DefaultMaxRetryAttempts = 2
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        var result = await _configService.UpdateSettingsAsync(settings);

        // Assert
        result.Should().BeTrue();
        settings.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        settings.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithStorageFailure_ShouldReturnFalse()
    {
        // Arrange
        var settings = new ConfigurationSettings();

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(false);

        // Act
        var result = await _configService.UpdateSettingsAsync(settings);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Reset Configuration Tests

    [Fact]
    public async Task ResetConfigurationAsync_ShouldResetAllConfiguration()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);
        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Version", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "profile1", "profile2" });
        _mockProfileService.Setup(x => x.DeleteProfileAsync("profile1"))
            .ReturnsAsync(true);
        _mockProfileService.Setup(x => x.DeleteProfileAsync("profile2"))
            .ReturnsAsync(true);

        // Act
        var result = await _configService.ResetConfigurationAsync();

        // Assert
        result.Should().BeTrue();
        _mockProfileService.Verify(x => x.DeleteProfileAsync("profile1"), Times.Once);
        _mockProfileService.Verify(x => x.DeleteProfileAsync("profile2"), Times.Once);
    }

    [Fact]
    public async Task ResetConfigurationAsync_WithFailure_ShouldReturnFalse()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(false); // Fail settings reset

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _configService.ResetConfigurationAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Export/Import Configuration Tests

    [Fact]
    public async Task ExportConfigurationAsync_WithValidConfiguration_ShouldReturnExportableConfiguration()
    {
        // Arrange
        var settings = new ConfigurationSettings
        {
            DefaultTimeoutSeconds = 45,
            DefaultMaxRetryAttempts = 5
        };

        var exportableProfile = new ExportableProfile
        {
            DisplayName = "Test Profile",
            InstanceUrl = "https://gitlab.com"
        };

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(settings),
                InstanceUrl = "settings-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "test-profile" });
        _mockProfileService.Setup(x => x.ExportProfileAsync("test-profile"))
            .ReturnsAsync(exportableProfile);

        // Act
        var result = await _configService.ExportConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Settings.Should().NotBeNull();
        result.Settings.DefaultTimeoutSeconds.Should().Be(45);
        result.Profiles.Should().HaveCount(1);
        result.Profiles[0].DisplayName.Should().Be("Test Profile");
        result.Metadata.Should().NotBeNull();
        result.Metadata.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExportConfigurationAsync_WithException_ShouldReturnNull()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        var result = await _configService.ExportConfigurationAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ImportConfigurationAsync_WithValidConfiguration_ShouldReturnSuccess()
    {
        // Arrange
        var importConfig = new ExportableConfiguration
        {
            Settings = new ConfigurationSettings
            {
                DefaultTimeoutSeconds = 60,
                DefaultMaxRetryAttempts = 2
            },
            Profiles = new List<ExportableProfile>
            {
                new ExportableProfile
                {
                    DisplayName = "Imported Profile",
                    InstanceUrl = "https://gitlab.example.com"
                }
            }
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());
        _mockProfileService.Setup(x => x.ImportProfileAsync(It.IsAny<ExportableProfile>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _configService.ImportConfigurationAsync(importConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SettingsImported.Should().BeTrue();
        result.ProfilesImported.Should().Be(1);
        result.ProfilesSkipped.Should().Be(0);
        result.ImportedItems.Should().Contain("Settings");
    }

    [Fact]
    public async Task ImportConfigurationAsync_WithExistingProfiles_ShouldSkipWithoutOverwrite()
    {
        // Arrange
        var importConfig = new ExportableConfiguration
        {
            Settings = new ConfigurationSettings(),
            Profiles = new List<ExportableProfile>
            {
                new ExportableProfile
                {
                    DisplayName = "Existing Profile",
                    InstanceUrl = "https://gitlab.com"
                }
            }
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "existing-profile" }); // Profile already exists
        _mockProfileService.Setup(x => x.ImportProfileAsync(It.IsAny<ExportableProfile>(), "existing-profile"))
            .ReturnsAsync(false);

        // Act
        var result = await _configService.ImportConfigurationAsync(importConfig, overwriteExisting: false);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProfilesSkipped.Should().Be(1);
        result.Warnings.Should().Contain("Skipped existing profile: existing-profile");
    }

    [Fact]
    public async Task ImportConfigurationAsync_WithOverwriteExisting_ShouldOverwriteProfiles()
    {
        // Arrange
        var importConfig = new ExportableConfiguration
        {
            Settings = new ConfigurationSettings(),
            Profiles = new List<ExportableProfile>
            {
                new ExportableProfile
                {
                    DisplayName = "Existing Profile",
                    InstanceUrl = "https://gitlab.com"
                }
            }
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Settings", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "existing-profile" });
        _mockProfileService.Setup(x => x.ImportProfileAsync(It.IsAny<ExportableProfile>(), "existing-profile"))
            .ReturnsAsync(true);

        // Act
        var result = await _configService.ImportConfigurationAsync(importConfig, overwriteExisting: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProfilesImported.Should().Be(1);
        result.ProfilesSkipped.Should().Be(0);
    }

    #endregion

    #region Schema Validation Tests

    [Fact]
    public async Task ValidateSchemaAsync_WithValidJson_ShouldReturnValid()
    {
        // Arrange
        var validJson = """
        {
            "settings": {
                "defaultTimeoutSeconds": 30
            },
            "profiles": [],
            "metadata": {
                "version": 1,
                "exportedAt": "2023-01-01T00:00:00Z"
            }
        }
        """;

        // Act
        var result = await _configService.ValidateSchemaAsync(validJson);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.DetectedVersion.Should().Be(1);
        result.ExpectedVersion.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateSchemaAsync_WithMissingProperties_ShouldReturnInvalid()
    {
        // Arrange
        var invalidJson = """
        {
            "settings": {
                "defaultTimeoutSeconds": 30
            }
        }
        """;

        // Act
        var result = await _configService.ValidateSchemaAsync(invalidJson);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Missing required property: profiles");
        result.Errors.Should().Contain("Missing required property: metadata");
    }

    [Fact]
    public async Task ValidateSchemaAsync_WithInvalidJson_ShouldReturnInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _configService.ValidateSchemaAsync(invalidJson);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.StartsWith("Invalid JSON:"));
    }

    #endregion

    #region Health Status Tests

    [Fact]
    public async Task GetHealthStatusAsync_WithHealthyConfiguration_ShouldReturnHealthy()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "test-profile",
            HasStoredCredentials = true
        };

        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "test-profile" });
        _mockProfileService.Setup(x => x.LoadProfileAsync("test-profile"))
            .ReturnsAsync(profile);
        _mockProfileService.Setup(x => x.GetDefaultProfileAsync())
            .ReturnsAsync(profile);

        // Act
        var result = await _configService.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.ConfigurationVersion.Should().Be(1);
        result.LatestVersion.Should().Be(1);
        result.ProfileCount.Should().Be(1);
        result.ProfilesWithCredentials.Should().Be(1);
        result.HasDefaultProfile.Should().BeTrue();
        result.CredentialStorageAvailable.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithOutdatedVersion_ShouldReturnWarning()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync((CredentialData?)null); // Version 0

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(new[] { "test-profile" });
        _mockProfileService.Setup(x => x.LoadProfileAsync("test-profile"))
            .ReturnsAsync(new ConfigurationProfile { Name = "test-profile" });
        _mockProfileService.Setup(x => x.GetDefaultProfileAsync())
            .ReturnsAsync(new ConfigurationProfile { Name = "test-profile" });

        // Act
        var result = await _configService.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Warning);
        result.ConfigurationVersion.Should().Be(0);
        result.Issues.Should().Contain("Configuration version (0) is outdated");
        result.Recommendations.Should().Contain("Run configuration migration to update to the latest version");
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithNoProfiles_ShouldReturnWarning()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "1",
                InstanceUrl = "version-marker"
            });

        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());
        _mockProfileService.Setup(x => x.GetDefaultProfileAsync())
            .ReturnsAsync((ConfigurationProfile?)null);

        // Act
        var result = await _configService.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Warning);
        result.ProfileCount.Should().Be(0);
        result.Issues.Should().Contain("No configuration profiles found");
        result.Recommendations.Should().Contain("Create at least one configuration profile");
    }

    [Fact]
    public async Task GetHealthStatusAsync_WithException_ShouldReturnError()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        var result = await _configService.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Error);
        result.Issues.Should().Contain("Health check error: Storage error");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldLogInformationMessages()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.IsAvailable).Returns(true);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync(It.IsAny<string>()))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = JsonSerializer.Serialize(new ConfigurationSettings()),
                InstanceUrl = "settings-marker"
            });
        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        await _configService.ValidateConfigurationAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuration validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MigrateConfigurationAsync_ShouldLogMigrationProgress()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Version"))
            .ReturnsAsync((CredentialData?)null);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Settings"))
            .ReturnsAsync((CredentialData?)null);
        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()))
            .ReturnsAsync(true);
        _mockProfileService.Setup(x => x.ListProfilesAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        await _configService.MigrateConfigurationAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting configuration migration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuration migration completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}