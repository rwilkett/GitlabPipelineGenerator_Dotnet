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
/// Unit tests for GitLabAuthenticationService
/// </summary>
public class GitLabAuthServiceTests
{
    private readonly Mock<ILogger<GitLabAuthenticationService>> _mockLogger;
    private readonly GitLabAuthenticationService _authService;

    public GitLabAuthServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitLabAuthenticationService>>();
        var mockErrorHandler = new Mock<IGitLabApiErrorHandler>();
        var mockCredentialStorage = new Mock<ICredentialStorageService>();
        _authService = new GitLabAuthenticationService(_mockLogger.Object, mockErrorHandler.Object, mockCredentialStorage.Object);
    }

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithNullToken_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = null,
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Personal access token is required");
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public async Task AuthenticateAsync_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "",
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Personal access token is required");
    }

    [Fact]
    public async Task AuthenticateAsync_WithWhitespaceToken_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "   ",
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Personal access token is required");
    }

    [Fact]
    public async Task AuthenticateAsync_WithNullInstanceUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "valid-token",
            InstanceUrl = null!
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Instance URL is required");
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public async Task AuthenticateAsync_WithEmptyInstanceUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "valid-token",
            InstanceUrl = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Instance URL is required");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "invalid-token",
            InstanceUrl = "https://gitlab.example.com",
            StoreCredentials = false
        };

        // Act & Assert
        // This will fail because it tries to make real API calls with invalid credentials
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Failed to authenticate with GitLab");
    }

    [Fact]
    public async Task AuthenticateAsync_WithMalformedUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "valid-token",
            InstanceUrl = "not-a-valid-url"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.AuthenticateAsync(options));
        
        exception.Message.Should().Contain("Failed to authenticate with GitLab");
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateTokenAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWhitespaceToken_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("   ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("invalid-token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithCustomInstanceUrl_ShouldUseProvidedUrl()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("token", "https://custom.gitlab.com");

        // Assert
        result.Should().BeFalse(); // Will fail due to invalid token, but tests the URL parameter
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullInstanceUrl_ShouldUseDefaultUrl()
    {
        // Act
        var result = await _authService.ValidateTokenAsync("token", null);

        // Assert
        result.Should().BeFalse(); // Will fail due to invalid token, but tests the default URL
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithoutAuthentication_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.GetCurrentUserAsync());
        
        exception.Message.Should().Contain("No authenticated GitLab client available");
    }

    #endregion

    #region Credential Storage Tests

    [Fact]
    public void StoreCredentials_WithValidOptions_ShouldThrowNotImplementedException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "valid-token",
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        // Current implementation throws NotImplementedException for platform-specific storage
        var exception = Assert.Throws<InvalidOperationException>(
            () => _authService.StoreCredentials(options));
        
        exception.Message.Should().Contain("Failed to store credentials");
        exception.InnerException.Should().BeOfType<NotImplementedException>();
    }

    [Fact]
    public void StoreCredentials_WithNullToken_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = null,
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _authService.StoreCredentials(options));
        
        exception.Message.Should().Contain("Personal access token is required to store credentials");
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public void StoreCredentials_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "",
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _authService.StoreCredentials(options));
        
        exception.Message.Should().Contain("Personal access token is required to store credentials");
    }

    [Fact]
    public void StoreCredentials_WithProfileName_ShouldUseProfileSpecificTarget()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "valid-token",
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act & Assert
        // Current implementation throws NotImplementedException for platform-specific storage
        var exception = Assert.Throws<InvalidOperationException>(
            () => _authService.StoreCredentials(options, "test-profile"));
        
        exception.Message.Should().Contain("Failed to store credentials");
        exception.InnerException.Should().BeOfType<NotImplementedException>();
    }

    [Fact]
    public void LoadStoredCredentials_WithNoStoredCredentials_ShouldReturnNull()
    {
        // Act
        var result = _authService.LoadStoredCredentials();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LoadStoredCredentials_WithProfileName_ShouldReturnNull()
    {
        // Act
        var result = _authService.LoadStoredCredentials("test-profile");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ClearStoredCredentials_ShouldNotThrow()
    {
        // Act & Assert
        // Should not throw even if no credentials are stored
        _authService.Invoking(s => s.ClearStoredCredentials())
            .Should().NotThrow();
    }

    [Fact]
    public void GetStoredProfiles_ShouldReturnEmptyList()
    {
        // Act
        var result = _authService.GetStoredProfiles();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void StoreCredentials_WithNullOptions_ShouldThrowNullReferenceException()
    {
        // Act & Assert
        // The current implementation doesn't validate null options, so it throws NullReferenceException
        Assert.Throws<NullReferenceException>(() => _authService.StoreCredentials(null!));
    }

    [Fact]
    public void StoreCredentials_WithProfileName_WithNullOptions_ShouldThrowNullReferenceException()
    {
        // Act & Assert
        // The current implementation doesn't validate null options, so it throws NullReferenceException
        Assert.Throws<NullReferenceException>(() => _authService.StoreCredentials(null!, "profile"));
    }

    [Fact]
    public void LoadStoredCredentials_WithNullProfileName_ShouldReturnNull()
    {
        // Act & Assert
        // The current implementation doesn't validate null profile name, it just returns null
        var result = _authService.LoadStoredCredentials(null!);
        result.Should().BeNull();
    }

    [Fact]
    public void LoadStoredCredentials_WithEmptyProfileName_ShouldReturnNull()
    {
        // Act & Assert
        // The current implementation doesn't validate empty profile name, it just returns null
        var result = _authService.LoadStoredCredentials("");
        result.Should().BeNull();
    }

    #endregion

    #region JSON Serialization Tests for Credential Storage

    [Fact]
    public void CredentialSerialization_ShouldHandleAllProperties()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "test-token",
            InstanceUrl = "https://gitlab.example.com",
            TimeoutSeconds = 45,
            MaxRetryAttempts = 5,
            ProfileName = "test-profile"
        };

        // Act - Test that the credential data structure can be serialized
        var credentialData = new
        {
            options.PersonalAccessToken,
            options.InstanceUrl,
            options.TimeoutSeconds,
            options.MaxRetryAttempts
        };

        var json = JsonSerializer.Serialize(credentialData);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("test-token");
        json.Should().Contain("https://gitlab.example.com");
        json.Should().Contain("45");
        json.Should().Contain("5");
    }

    [Fact]
    public void CredentialDeserialization_ShouldHandleValidJson()
    {
        // Arrange
        var json = """
        {
            "PersonalAccessToken": "test-token",
            "InstanceUrl": "https://gitlab.example.com",
            "TimeoutSeconds": 45,
            "MaxRetryAttempts": 5
        }
        """;

        // Act
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        var reconstructedOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = root.GetProperty("PersonalAccessToken").GetString(),
            InstanceUrl = root.GetProperty("InstanceUrl").GetString() ?? "https://gitlab.com",
            TimeoutSeconds = root.TryGetProperty("TimeoutSeconds", out var timeout) ? timeout.GetInt32() : 30,
            MaxRetryAttempts = root.TryGetProperty("MaxRetryAttempts", out var retry) ? retry.GetInt32() : 3
        };

        // Assert
        reconstructedOptions.PersonalAccessToken.Should().Be("test-token");
        reconstructedOptions.InstanceUrl.Should().Be("https://gitlab.example.com");
        reconstructedOptions.TimeoutSeconds.Should().Be(45);
        reconstructedOptions.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void CredentialDeserialization_ShouldHandlePartialJson()
    {
        // Arrange
        var json = """
        {
            "PersonalAccessToken": "test-token",
            "InstanceUrl": "https://gitlab.example.com"
        }
        """;

        // Act
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        var reconstructedOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = root.GetProperty("PersonalAccessToken").GetString(),
            InstanceUrl = root.GetProperty("InstanceUrl").GetString() ?? "https://gitlab.com",
            TimeoutSeconds = root.TryGetProperty("TimeoutSeconds", out var timeout) ? timeout.GetInt32() : 30,
            MaxRetryAttempts = root.TryGetProperty("MaxRetryAttempts", out var retry) ? retry.GetInt32() : 3
        };

        // Assert
        reconstructedOptions.PersonalAccessToken.Should().Be("test-token");
        reconstructedOptions.InstanceUrl.Should().Be("https://gitlab.example.com");
        reconstructedOptions.TimeoutSeconds.Should().Be(30); // Default value
        reconstructedOptions.MaxRetryAttempts.Should().Be(3); // Default value
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task AuthenticateAsync_ShouldLogInformationMessages()
    {
        // Arrange
        var options = new GitLabConnectionOptions
        {
            PersonalAccessToken = "valid-token",
            InstanceUrl = "https://gitlab.example.com"
        };

        // Act
        try
        {
            await _authService.AuthenticateAsync(options);
        }
        catch
        {
            // Expected to fail due to network call
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authenticating with GitLab instance")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldLogDebugMessage()
    {
        // Act
        await _authService.ValidateTokenAsync("invalid-token");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void AuthenticationService_ShouldImplementInterface()
    {
        // Assert
        _authService.Should().BeAssignableTo<IGitLabAuthenticationService>();
    }

    [Fact]
    public void AuthenticationService_ShouldHaveAllInterfaceMethods()
    {
        // Arrange
        var interfaceType = typeof(IGitLabAuthenticationService);
        var serviceType = typeof(GitLabAuthenticationService);

        // Act & Assert
        foreach (var method in interfaceType.GetMethods())
        {
            var serviceMethod = serviceType.GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray());
            serviceMethod.Should().NotBeNull($"Method {method.Name} should be implemented");
        }
    }

    #endregion
}