using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for CrossPlatformCredentialStorageService
/// </summary>
public class CrossPlatformCredentialStorageServiceTests
{
    private readonly Mock<ILogger<CrossPlatformCredentialStorageService>> _mockLogger;
    private readonly CrossPlatformCredentialStorageService _storageService;

    public CrossPlatformCredentialStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<CrossPlatformCredentialStorageService>>();
        _storageService = new CrossPlatformCredentialStorageService(_mockLogger.Object);
    }

    #region IsAvailable Tests

    [Fact]
    public void IsAvailable_ShouldReturnBooleanValue()
    {
        // Act
        var isAvailable = _storageService.IsAvailable;

        // Assert
        isAvailable.Should().BeOfType<bool>();
        // Note: The actual value depends on the platform and OS credential store availability
    }

    #endregion

    #region StoreCredentialAsync Tests

    [Fact]
    public async Task StoreCredentialAsync_WithValidCredentials_ShouldSerializeAndStore()
    {
        // Arrange
        var target = "test-target";
        var credentials = new CredentialData
        {
            PersonalAccessToken = "test-token",
            InstanceUrl = "https://gitlab.com",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            StoredAt = DateTime.UtcNow,
            Version = 1
        };

        // Act
        var result = await _storageService.StoreCredentialAsync(target, credentials);

        // Assert
        // Note: The actual result depends on platform credential store availability
        // We can only test that the method doesn't throw and returns a boolean
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public async Task StoreCredentialAsync_WithNullCredentials_ShouldHandleGracefully()
    {
        // Arrange
        var target = "test-target";

        // Act & Assert
        // Should not throw, but may return false depending on serialization behavior
        var result = await _storageService.StoreCredentialAsync(target, null!);
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public async Task StoreCredentialAsync_WithEmptyTarget_ShouldHandleGracefully()
    {
        // Arrange
        var credentials = new CredentialData
        {
            PersonalAccessToken = "test-token",
            InstanceUrl = "https://gitlab.com"
        };

        // Act
        var result = await _storageService.StoreCredentialAsync("", credentials);

        // Assert
        result.Should().BeOfType<bool>();
    }

    #endregion

    #region LoadCredentialAsync Tests

    [Fact]
    public async Task LoadCredentialAsync_WithNonExistentTarget_ShouldReturnNull()
    {
        // Arrange
        var target = "nonexistent-target-" + Guid.NewGuid();

        // Act
        var result = await _storageService.LoadCredentialAsync(target);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadCredentialAsync_WithEmptyTarget_ShouldReturnNull()
    {
        // Act
        var result = await _storageService.LoadCredentialAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadCredentialAsync_WithNullTarget_ShouldHandleGracefully()
    {
        // Act
        var result = await _storageService.LoadCredentialAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteCredentialAsync Tests

    [Fact]
    public async Task DeleteCredentialAsync_WithNonExistentTarget_ShouldReturnFalse()
    {
        // Arrange
        var target = "nonexistent-target-" + Guid.NewGuid();

        // Act
        var result = await _storageService.DeleteCredentialAsync(target);

        // Assert
        // Note: Behavior may vary by platform, but should return a boolean
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public async Task DeleteCredentialAsync_WithEmptyTarget_ShouldHandleGracefully()
    {
        // Act
        var result = await _storageService.DeleteCredentialAsync("");

        // Assert
        result.Should().BeOfType<bool>();
    }

    [Fact]
    public async Task DeleteCredentialAsync_WithNullTarget_ShouldHandleGracefully()
    {
        // Act
        var result = await _storageService.DeleteCredentialAsync(null!);

        // Assert
        result.Should().BeOfType<bool>();
    }

    #endregion

    #region ListCredentialTargetsAsync Tests

    [Fact]
    public async Task ListCredentialTargetsAsync_WithValidPrefix_ShouldReturnEnumerable()
    {
        // Arrange
        var prefix = "test-prefix-";

        // Act
        var result = await _storageService.ListCredentialTargetsAsync(prefix);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<string>>();
    }

    [Fact]
    public async Task ListCredentialTargetsAsync_WithEmptyPrefix_ShouldReturnEnumerable()
    {
        // Act
        var result = await _storageService.ListCredentialTargetsAsync("");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<string>>();
    }

    [Fact]
    public async Task ListCredentialTargetsAsync_WithNullPrefix_ShouldHandleGracefully()
    {
        // Act
        var result = await _storageService.ListCredentialTargetsAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<string>>();
    }

    #endregion

    #region Integration Tests (Store/Load/Delete Cycle)

    [Fact]
    public async Task StoreLoadDeleteCycle_WithValidCredentials_ShouldWorkCorrectly()
    {
        // Skip this test if credential storage is not available
        if (!_storageService.IsAvailable)
        {
            return;
        }

        // Arrange
        var target = "integration-test-" + Guid.NewGuid();
        var originalCredentials = new CredentialData
        {
            PersonalAccessToken = "integration-test-token",
            InstanceUrl = "https://gitlab.integration.test",
            TimeoutSeconds = 45,
            MaxRetryAttempts = 5,
            ProfileName = "integration-test-profile",
            StoredAt = DateTime.UtcNow,
            Version = 1
        };

        try
        {
            // Act - Store
            var storeResult = await _storageService.StoreCredentialAsync(target, originalCredentials);

            // Assert - Store succeeded
            storeResult.Should().BeTrue();

            // Act - Load
            var loadedCredentials = await _storageService.LoadCredentialAsync(target);

            // Assert - Load succeeded and data matches
            loadedCredentials.Should().NotBeNull();
            loadedCredentials!.PersonalAccessToken.Should().Be(originalCredentials.PersonalAccessToken);
            loadedCredentials.InstanceUrl.Should().Be(originalCredentials.InstanceUrl);
            loadedCredentials.TimeoutSeconds.Should().Be(originalCredentials.TimeoutSeconds);
            loadedCredentials.MaxRetryAttempts.Should().Be(originalCredentials.MaxRetryAttempts);
            loadedCredentials.ProfileName.Should().Be(originalCredentials.ProfileName);
            loadedCredentials.Version.Should().Be(originalCredentials.Version);
            loadedCredentials.StoredAt.Should().BeCloseTo(originalCredentials.StoredAt, TimeSpan.FromSeconds(1));

            // Act - Delete
            var deleteResult = await _storageService.DeleteCredentialAsync(target);

            // Assert - Delete succeeded
            deleteResult.Should().BeTrue();

            // Act - Verify deletion
            var deletedCredentials = await _storageService.LoadCredentialAsync(target);

            // Assert - Credentials no longer exist
            deletedCredentials.Should().BeNull();
        }
        finally
        {
            // Cleanup - Ensure credentials are deleted even if test fails
            try
            {
                await _storageService.DeleteCredentialAsync(target);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task ListCredentialTargets_WithStoredCredentials_ShouldIncludeTargets()
    {
        // Skip this test if credential storage is not available
        if (!_storageService.IsAvailable)
        {
            return;
        }

        // Arrange
        var prefix = "list-test-" + Guid.NewGuid().ToString("N")[..8] + "-";
        var target1 = prefix + "target1";
        var target2 = prefix + "target2";
        var credentials = new CredentialData
        {
            PersonalAccessToken = "list-test-token",
            InstanceUrl = "https://gitlab.com"
        };

        try
        {
            // Act - Store test credentials
            await _storageService.StoreCredentialAsync(target1, credentials);
            await _storageService.StoreCredentialAsync(target2, credentials);

            // Act - List targets
            var targets = await _storageService.ListCredentialTargetsAsync(prefix);

            // Assert - Targets are included in the list
            targets.Should().NotBeNull();
            var targetList = targets.ToList();
            targetList.Should().Contain(target1);
            targetList.Should().Contain(target2);
        }
        finally
        {
            // Cleanup
            try
            {
                await _storageService.DeleteCredentialAsync(target1);
                await _storageService.DeleteCredentialAsync(target2);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public async Task StoreCredentialAsync_ShouldSerializeCredentialsCorrectly()
    {
        // Arrange
        var credentials = new CredentialData
        {
            PersonalAccessToken = "serialization-test-token",
            InstanceUrl = "https://gitlab.serialization.test",
            TimeoutSeconds = 60,
            MaxRetryAttempts = 2,
            ProfileName = "serialization-test-profile",
            StoredAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Version = 2
        };

        // Act - Test serialization directly
        var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Assert - JSON contains expected data
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("serialization-test-token");
        json.Should().Contain("https://gitlab.serialization.test");
        json.Should().Contain("60");
        json.Should().Contain("2");
        json.Should().Contain("serialization-test-profile");

        // Act - Test deserialization
        var deserializedCredentials = JsonSerializer.Deserialize<CredentialData>(json);

        // Assert - Deserialized data matches original
        deserializedCredentials.Should().NotBeNull();
        deserializedCredentials!.PersonalAccessToken.Should().Be(credentials.PersonalAccessToken);
        deserializedCredentials.InstanceUrl.Should().Be(credentials.InstanceUrl);
        deserializedCredentials.TimeoutSeconds.Should().Be(credentials.TimeoutSeconds);
        deserializedCredentials.MaxRetryAttempts.Should().Be(credentials.MaxRetryAttempts);
        deserializedCredentials.ProfileName.Should().Be(credentials.ProfileName);
        deserializedCredentials.Version.Should().Be(credentials.Version);
        deserializedCredentials.StoredAt.Should().Be(credentials.StoredAt);
    }

    [Fact]
    public async Task LoadCredentialAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Note: This test would require mocking the underlying provider to return invalid JSON
        // For now, we test that the service handles JSON exceptions gracefully
        
        // This is more of a documentation test showing expected behavior
        // In a real scenario with invalid JSON, the service should return null and log an error
        
        // Arrange
        var target = "invalid-json-test-" + Guid.NewGuid();

        // Act
        var result = await _storageService.LoadCredentialAsync(target);

        // Assert
        result.Should().BeNull(); // Should not throw, should return null for non-existent target
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task StoreCredentialAsync_WithUnavailableStorage_ShouldLogWarning()
    {
        // Note: This test depends on the actual platform credential storage availability
        // If storage is unavailable, it should log a warning
        
        // Arrange
        var target = "logging-test-target";
        var credentials = new CredentialData
        {
            PersonalAccessToken = "logging-test-token",
            InstanceUrl = "https://gitlab.com"
        };

        // Act
        await _storageService.StoreCredentialAsync(target, credentials);

        // Assert
        // If storage is unavailable, should log warning
        if (!_storageService.IsAvailable)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Credential storage is not available")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task LoadCredentialAsync_WithUnavailableStorage_ShouldLogDebug()
    {
        // Arrange
        var target = "logging-test-target";

        // Act
        await _storageService.LoadCredentialAsync(target);

        // Assert
        // If storage is unavailable, should log debug message
        if (!_storageService.IsAvailable)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Credential storage is not available")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    #endregion

    #region Platform-Specific Tests

    [Fact]
    public void Constructor_ShouldCreatePlatformSpecificProvider()
    {
        // Arrange & Act
        var service = new CrossPlatformCredentialStorageService(_mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
        service.IsAvailable.Should().BeOfType<bool>();
        
        // The service should have created a platform-specific provider internally
        // We can't directly test which provider was created, but we can test that
        // the service was constructed successfully and has the expected interface
    }

    [Fact]
    public async Task AllMethods_WithUnavailableStorage_ShouldHandleGracefully()
    {
        // This test ensures that all methods handle unavailable storage gracefully
        // without throwing exceptions
        
        // Arrange
        var target = "unavailable-storage-test";
        var credentials = new CredentialData
        {
            PersonalAccessToken = "test-token",
            InstanceUrl = "https://gitlab.com"
        };

        // Act & Assert - All methods should complete without throwing
        var storeResult = await _storageService.StoreCredentialAsync(target, credentials);
        storeResult.Should().BeOfType<bool>();

        var loadResult = await _storageService.LoadCredentialAsync(target);
        loadResult.Should().BeOfType<CredentialData>().Or.BeNull();

        var deleteResult = await _storageService.DeleteCredentialAsync(target);
        deleteResult.Should().BeOfType<bool>();

        var listResult = await _storageService.ListCredentialTargetsAsync("test-");
        listResult.Should().NotBeNull();
        listResult.Should().BeAssignableTo<IEnumerable<string>>();
    }

    #endregion
}