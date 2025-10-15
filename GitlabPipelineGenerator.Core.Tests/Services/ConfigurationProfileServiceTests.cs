using FluentAssertions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationProfileService
/// </summary>
public class ConfigurationProfileServiceTests
{
    private readonly Mock<ILogger<ConfigurationProfileService>> _mockLogger;
    private readonly Mock<ICredentialStorageService> _mockCredentialStorage;
    private readonly Mock<IGitLabAuthenticationService> _mockAuthService;
    private readonly ConfigurationProfileService _profileService;

    public ConfigurationProfileServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationProfileService>>();
        _mockCredentialStorage = new Mock<ICredentialStorageService>();
        _mockAuthService = new Mock<IGitLabAuthenticationService>();
        
        _profileService = new ConfigurationProfileService(
            _mockLogger.Object,
            _mockCredentialStorage.Object,
            _mockAuthService.Object);
    }

    #region SaveProfileAsync Tests

    [Fact]
    public async Task SaveProfileAsync_WithValidProfile_ShouldReturnTrue()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "test-profile",
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync(
            "GitLabPipelineGenerator_Config_test-profile", 
            It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.SaveProfileAsync(profile);

        // Assert
        result.Should().BeTrue();
        profile.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync(
            "GitLabPipelineGenerator_Config_test-profile",
            It.Is<CredentialData>(c => 
                c.InstanceUrl == "https://gitlab.com" &&
                c.TimeoutSeconds == 30 &&
                c.MaxRetryAttempts == 3 &&
                c.ProfileName == "test-profile")), Times.Once);
    }

    [Fact]
    public async Task SaveProfileAsync_WithInvalidProfile_ShouldReturnFalse()
    {
        // Arrange
        var invalidProfile = new ConfigurationProfile
        {
            Name = "", // Invalid name
            InstanceUrl = "invalid-url", // Invalid URL
            TimeoutSeconds = -1, // Invalid timeout
            MaxRetryAttempts = 15 // Invalid retry attempts
        };

        // Act
        var result = await _profileService.SaveProfileAsync(invalidProfile);

        // Assert
        result.Should().BeFalse();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()), Times.Never);
    }

    [Fact]
    public async Task SaveProfileAsync_WithDefaultProfile_ShouldSetAsDefault()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "default-profile",
            InstanceUrl = "https://gitlab.com",
            IsDefault = true
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.SaveProfileAsync(profile);

        // Assert
        result.Should().BeTrue();
        
        // Verify that SetDefaultProfileAsync was called
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync(
            "GitLabPipelineGenerator_DefaultProfile",
            It.Is<CredentialData>(c => c.PersonalAccessToken == "default-profile")), Times.Once);
    }

    [Fact]
    public async Task SaveProfileAsync_WithStorageFailure_ShouldReturnFalse()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "test-profile",
            InstanceUrl = "https://gitlab.com"
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()))
            .ReturnsAsync(false);

        // Act
        var result = await _profileService.SaveProfileAsync(profile);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LoadProfileAsync Tests

    [Fact]
    public async Task LoadProfileAsync_WithExistingProfile_ShouldReturnProfile()
    {
        // Arrange
        var credentialData = new CredentialData
        {
            InstanceUrl = "https://gitlab.example.com",
            TimeoutSeconds = 45,
            MaxRetryAttempts = 5,
            ProfileName = "test-profile",
            StoredAt = DateTime.UtcNow.AddDays(-1)
        };

        var storedCredentials = new GitLabConnectionOptions
        {
            PersonalAccessToken = "stored-token",
            InstanceUrl = "https://gitlab.example.com"
        };

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(credentialData);

        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns(storedCredentials);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "test-profile",
                InstanceUrl = "default-profile-marker"
            });

        // Act
        var result = await _profileService.LoadProfileAsync("test-profile");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-profile");
        result.InstanceUrl.Should().Be("https://gitlab.example.com");
        result.TimeoutSeconds.Should().Be(45);
        result.MaxRetryAttempts.Should().Be(5);
        result.HasStoredCredentials.Should().BeTrue();
        result.IsDefault.Should().BeTrue();
        result.CreatedAt.Should().Be(credentialData.StoredAt);
        result.LastModified.Should().Be(credentialData.StoredAt);
    }

    [Fact]
    public async Task LoadProfileAsync_WithNonExistentProfile_ShouldReturnNull()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_nonexistent"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _profileService.LoadProfileAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadProfileAsync_WithEmptyName_ShouldReturnNull()
    {
        // Act
        var result = await _profileService.LoadProfileAsync("");

        // Assert
        result.Should().BeNull();
        _mockCredentialStorage.Verify(x => x.LoadCredentialAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadProfileAsync_WithException_ShouldReturnNull()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_error-profile"))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        var result = await _profileService.LoadProfileAsync("error-profile");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_WithExistingProfile_ShouldReturnTrue()
    {
        // Arrange
        var storedCredentials = new GitLabConnectionOptions
        {
            PersonalAccessToken = "stored-token"
        };

        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(true);

        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns(storedCredentials);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync((CredentialData?)null); // Not the default profile

        // Act
        var result = await _profileService.DeleteProfileAsync("test-profile");

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_Config_test-profile"), Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithDefaultProfile_ShouldClearDefault()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_Config_default-profile"))
            .ReturnsAsync(true);

        _mockAuthService.Setup(x => x.LoadStoredCredentials("default-profile"))
            .Returns((GitLabConnectionOptions?)null);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync(new CredentialData
            {
                PersonalAccessToken = "default-profile",
                InstanceUrl = "default-profile-marker"
            });

        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.DeleteProfileAsync("default-profile");

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_DefaultProfile"), Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithEmptyName_ShouldReturnFalse()
    {
        // Act
        var result = await _profileService.DeleteProfileAsync("");

        // Assert
        result.Should().BeFalse();
        _mockCredentialStorage.Verify(x => x.DeleteCredentialAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithStorageFailure_ShouldReturnFalse()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(false);

        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns((GitLabConnectionOptions?)null);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _profileService.DeleteProfileAsync("test-profile");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ListProfilesAsync Tests

    [Fact]
    public async Task ListProfilesAsync_WithExistingProfiles_ShouldReturnProfileNames()
    {
        // Arrange
        var targets = new[]
        {
            "GitLabPipelineGenerator_Config_profile1",
            "GitLabPipelineGenerator_Config_profile2",
            "GitLabPipelineGenerator_Config_profile3",
            "SomeOtherTarget" // Should be filtered out
        };

        _mockCredentialStorage.Setup(x => x.ListCredentialTargetsAsync("GitLabPipelineGenerator_Config_"))
            .ReturnsAsync(targets);

        // Act
        var result = await _profileService.ListProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("profile1");
        result.Should().Contain("profile2");
        result.Should().Contain("profile3");
        result.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ListProfilesAsync_WithNoProfiles_ShouldReturnEmpty()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.ListCredentialTargetsAsync("GitLabPipelineGenerator_Config_"))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _profileService.ListProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListProfilesAsync_WithException_ShouldReturnEmpty()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.ListCredentialTargetsAsync("GitLabPipelineGenerator_Config_"))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        var result = await _profileService.ListProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Default Profile Tests

    [Fact]
    public async Task GetDefaultProfileAsync_WithDefaultProfile_ShouldReturnProfile()
    {
        // Arrange
        var defaultProfileData = new CredentialData
        {
            PersonalAccessToken = "test-profile",
            InstanceUrl = "default-profile-marker"
        };

        var profileData = new CredentialData
        {
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            ProfileName = "test-profile",
            StoredAt = DateTime.UtcNow
        };

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync(defaultProfileData);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(profileData);

        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns((GitLabConnectionOptions?)null);

        // Act
        var result = await _profileService.GetDefaultProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-profile");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultProfileAsync_WithNoDefaultProfile_ShouldReturnNull()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _profileService.GetDefaultProfileAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetDefaultProfileAsync_WithValidProfile_ShouldReturnTrue()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "test-profile",
            InstanceUrl = "https://gitlab.com"
        };

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(new CredentialData
            {
                InstanceUrl = "https://gitlab.com",
                ProfileName = "test-profile",
                StoredAt = DateTime.UtcNow
            });

        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns((GitLabConnectionOptions?)null);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync((CredentialData?)null);

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_DefaultProfile", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.SetDefaultProfileAsync("test-profile");

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync("GitLabPipelineGenerator_DefaultProfile",
            It.Is<CredentialData>(c => c.PersonalAccessToken == "test-profile")), Times.Once);
    }

    [Fact]
    public async Task SetDefaultProfileAsync_WithNonExistentProfile_ShouldReturnFalse()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_nonexistent"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _profileService.SetDefaultProfileAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync("GitLabPipelineGenerator_DefaultProfile", It.IsAny<CredentialData>()), Times.Never);
    }

    [Fact]
    public async Task SetDefaultProfileAsync_WithEmptyName_ShouldClearDefault()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.SetDefaultProfileAsync("");

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_DefaultProfile"), Times.Once);
    }

    #endregion

    #region ValidateProfileAsync Tests

    [Fact]
    public async Task ValidateProfileAsync_WithValidProfile_ShouldReturnSuccess()
    {
        // Arrange
        var validProfile = new ConfigurationProfile
        {
            Name = "valid-profile",
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(validProfile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateProfileAsync_WithInvalidName_ShouldReturnFailure()
    {
        // Arrange
        var invalidProfile = new ConfigurationProfile
        {
            Name = "", // Invalid name
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(invalidProfile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Profile name is required");
    }

    [Fact]
    public async Task ValidateProfileAsync_WithInvalidCharactersInName_ShouldReturnFailure()
    {
        // Arrange
        var invalidProfile = new ConfigurationProfile
        {
            Name = "invalid@name#", // Invalid characters
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(invalidProfile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Profile name contains invalid characters. Use only letters, numbers, hyphens, and underscores");
    }

    [Fact]
    public async Task ValidateProfileAsync_WithInvalidUrl_ShouldReturnFailure()
    {
        // Arrange
        var invalidProfile = new ConfigurationProfile
        {
            Name = "valid-profile",
            InstanceUrl = "not-a-url", // Invalid URL
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(invalidProfile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Instance URL must be a valid HTTP or HTTPS URL");
    }

    [Fact]
    public async Task ValidateProfileAsync_WithInvalidTimeout_ShouldReturnFailure()
    {
        // Arrange
        var invalidProfile = new ConfigurationProfile
        {
            Name = "valid-profile",
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = -1, // Invalid timeout
            MaxRetryAttempts = 3,
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(invalidProfile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Timeout must be between 1 and 300 seconds");
    }

    [Fact]
    public async Task ValidateProfileAsync_WithInvalidRetryAttempts_ShouldReturnFailure()
    {
        // Arrange
        var invalidProfile = new ConfigurationProfile
        {
            Name = "valid-profile",
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 15, // Invalid retry attempts
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(invalidProfile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Max retry attempts must be between 0 and 10");
    }

    [Fact]
    public async Task ValidateProfileAsync_WithWarningConditions_ShouldReturnWarnings()
    {
        // Arrange
        var profileWithWarnings = new ConfigurationProfile
        {
            Name = "valid-profile",
            DisplayName = "Custom GitLab", // Doesn't indicate gitlab.com
            InstanceUrl = "https://gitlab.com", // But URL is gitlab.com
            TimeoutSeconds = 5, // Very low timeout
            MaxRetryAttempts = 0, // No retries
            Version = 1
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(profileWithWarnings);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("Profile appears to be for GitLab.com but display name doesn't indicate this");
        result.Warnings.Should().Contain("Timeout is very low and may cause connection issues");
        result.Warnings.Should().Contain("No retry attempts configured - API calls may fail on temporary network issues");
    }

    [Fact]
    public async Task ValidateProfileAsync_WithException_ShouldReturnFailure()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = null!, // This will cause an exception
            InstanceUrl = "https://gitlab.com"
        };

        // Act
        var result = await _profileService.ValidateProfileAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.StartsWith("Validation error:"));
    }

    #endregion

    #region Export/Import Profile Tests

    [Fact]
    public async Task ExportProfileAsync_WithExistingProfile_ShouldReturnExportableProfile()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "test-profile",
            DisplayName = "Test Profile",
            Description = "Test description",
            InstanceUrl = "https://gitlab.example.com",
            TimeoutSeconds = 45,
            MaxRetryAttempts = 5,
            Version = 1
        };

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(new CredentialData
            {
                InstanceUrl = profile.InstanceUrl,
                TimeoutSeconds = profile.TimeoutSeconds,
                MaxRetryAttempts = profile.MaxRetryAttempts,
                ProfileName = profile.Name,
                StoredAt = DateTime.UtcNow
            });

        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns((GitLabConnectionOptions?)null);

        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _profileService.ExportProfileAsync("test-profile");

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Test Profile");
        result.Description.Should().Be("Test description");
        result.InstanceUrl.Should().Be("https://gitlab.example.com");
        result.TimeoutSeconds.Should().Be(45);
        result.MaxRetryAttempts.Should().Be(5);
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task ExportProfileAsync_WithNonExistentProfile_ShouldReturnNull()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_nonexistent"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        var result = await _profileService.ExportProfileAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ImportProfileAsync_WithValidProfile_ShouldReturnTrue()
    {
        // Arrange
        var exportableProfile = new ExportableProfile
        {
            DisplayName = "Imported Profile",
            Description = "Imported description",
            InstanceUrl = "https://gitlab.imported.com",
            TimeoutSeconds = 60,
            MaxRetryAttempts = 2,
            Version = 1
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Config_imported-profile", It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.ImportProfileAsync(exportableProfile, "imported-profile");

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync("GitLabPipelineGenerator_Config_imported-profile",
            It.Is<CredentialData>(c => 
                c.InstanceUrl == "https://gitlab.imported.com" &&
                c.TimeoutSeconds == 60 &&
                c.MaxRetryAttempts == 2)), Times.Once);
    }

    [Fact]
    public async Task ImportProfileAsync_WithInvalidProfile_ShouldReturnFalse()
    {
        // Arrange
        var invalidExportableProfile = new ExportableProfile
        {
            DisplayName = "Invalid Profile",
            InstanceUrl = "invalid-url", // Invalid URL
            TimeoutSeconds = -1, // Invalid timeout
            Version = 1
        };

        // Act
        var result = await _profileService.ImportProfileAsync(invalidExportableProfile, "invalid-profile");

        // Assert
        result.Should().BeFalse();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()), Times.Never);
    }

    [Fact]
    public async Task ImportProfileAsync_WithEmptyName_ShouldReturnFalse()
    {
        // Arrange
        var exportableProfile = new ExportableProfile
        {
            DisplayName = "Valid Profile",
            InstanceUrl = "https://gitlab.com"
        };

        // Act
        var result = await _profileService.ImportProfileAsync(exportableProfile, "");

        // Assert
        result.Should().BeFalse();
        _mockCredentialStorage.Verify(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()), Times.Never);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task SaveProfileAsync_ShouldLogInformationMessages()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "test-profile",
            InstanceUrl = "https://gitlab.com"
        };

        _mockCredentialStorage.Setup(x => x.StoreCredentialAsync(It.IsAny<string>(), It.IsAny<CredentialData>()))
            .ReturnsAsync(true);

        // Act
        await _profileService.SaveProfileAsync(profile);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully saved configuration profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_ShouldLogInformationMessages()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_Config_test-profile"))
            .ReturnsAsync(true);
        _mockAuthService.Setup(x => x.LoadStoredCredentials("test-profile"))
            .Returns((GitLabConnectionOptions?)null);
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync((CredentialData?)null);

        // Act
        await _profileService.DeleteProfileAsync("test-profile");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully deleted configuration profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadProfileAsync_WithException_ShouldLogError()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.LoadCredentialAsync("GitLabPipelineGenerator_Config_error-profile"))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        await _profileService.LoadProfileAsync("error-profile");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error loading configuration profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task SaveProfileAsync_WithNullProfile_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _profileService.SaveProfileAsync(null!));
    }

    [Fact]
    public async Task ValidateProfileAsync_WithNullProfile_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _profileService.ValidateProfileAsync(null!));
    }

    [Fact]
    public async Task ImportProfileAsync_WithNullProfile_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _profileService.ImportProfileAsync(null!, "test"));
    }

    [Fact]
    public async Task LoadProfileAsync_WithWhitespaceOnlyName_ShouldReturnNull()
    {
        // Act
        var result = await _profileService.LoadProfileAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileAsync_WithWhitespaceOnlyName_ShouldReturnFalse()
    {
        // Act
        var result = await _profileService.DeleteProfileAsync("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultProfileAsync_WithNullName_ShouldClearDefault()
    {
        // Arrange
        _mockCredentialStorage.Setup(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_DefaultProfile"))
            .ReturnsAsync(true);

        // Act
        var result = await _profileService.SetDefaultProfileAsync(null!);

        // Assert
        result.Should().BeTrue();
        _mockCredentialStorage.Verify(x => x.DeleteCredentialAsync("GitLabPipelineGenerator_DefaultProfile"), Times.Once);
    }

    #endregion
}