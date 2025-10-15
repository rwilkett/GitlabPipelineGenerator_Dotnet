using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Cross-platform credential storage service that uses OS-specific credential stores
/// </summary>
public class CrossPlatformCredentialStorageService : ICredentialStorageService
{
    private readonly ILogger<CrossPlatformCredentialStorageService> _logger;
    private readonly ICredentialStorageProvider _provider;

    public CrossPlatformCredentialStorageService(ILogger<CrossPlatformCredentialStorageService> logger)
    {
        _logger = logger;
        _provider = CreatePlatformProvider();
    }

    /// <inheritdoc />
    public bool IsAvailable => _provider.IsAvailable;

    /// <inheritdoc />
    public async Task<bool> StoreCredentialAsync(string target, CredentialData credentials)
    {
        try
        {
            if (!IsAvailable)
            {
                _logger.LogWarning("Credential storage is not available on this platform");
                return false;
            }

            var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var success = await _provider.StoreCredentialAsync(target, json);
            
            if (success)
            {
                _logger.LogDebug("Successfully stored credentials for target: {Target}", target);
            }
            else
            {
                _logger.LogWarning("Failed to store credentials for target: {Target}", target);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing credentials for target: {Target}", target);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<CredentialData?> LoadCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
            {
                _logger.LogDebug("Credential storage is not available on this platform");
                return null;
            }

            var json = await _provider.LoadCredentialAsync(target);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogDebug("No credentials found for target: {Target}", target);
                return null;
            }

            var credentials = JsonSerializer.Deserialize<CredentialData>(json);
            
            if (credentials != null)
            {
                _logger.LogDebug("Successfully loaded credentials for target: {Target}", target);
            }

            return credentials;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing credentials for target: {Target}", target);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading credentials for target: {Target}", target);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
            {
                _logger.LogWarning("Credential storage is not available on this platform");
                return false;
            }

            var success = await _provider.DeleteCredentialAsync(target);
            
            if (success)
            {
                _logger.LogDebug("Successfully deleted credentials for target: {Target}", target);
            }
            else
            {
                _logger.LogWarning("Failed to delete credentials for target: {Target}", target);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credentials for target: {Target}", target);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix)
    {
        try
        {
            if (!IsAvailable)
            {
                _logger.LogDebug("Credential storage is not available on this platform");
                return Enumerable.Empty<string>();
            }

            var targets = await _provider.ListCredentialTargetsAsync(targetPrefix);
            
            _logger.LogDebug("Found {Count} credential targets with prefix: {Prefix}", 
                targets.Count(), targetPrefix);

            return targets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing credential targets with prefix: {Prefix}", targetPrefix);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Creates the appropriate credential storage provider for the current platform
    /// </summary>
    private ICredentialStorageProvider CreatePlatformProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsCredentialStorageProvider(_logger);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOSCredentialStorageProvider(_logger);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxCredentialStorageProvider(_logger);
        }
        else
        {
            _logger.LogWarning("Unsupported platform for credential storage: {Platform}", 
                RuntimeInformation.OSDescription);
            return new NullCredentialStorageProvider();
        }
    }
}

/// <summary>
/// Platform-specific credential storage provider interface
/// </summary>
internal interface ICredentialStorageProvider
{
    bool IsAvailable { get; }
    Task<bool> StoreCredentialAsync(string target, string data);
    Task<string?> LoadCredentialAsync(string target);
    Task<bool> DeleteCredentialAsync(string target);
    Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix);
}

/// <summary>
/// Null credential storage provider for unsupported platforms
/// </summary>
internal class NullCredentialStorageProvider : ICredentialStorageProvider
{
    public bool IsAvailable => false;

    public Task<bool> StoreCredentialAsync(string target, string data) => Task.FromResult(false);
    public Task<string?> LoadCredentialAsync(string target) => Task.FromResult<string?>(null);
    public Task<bool> DeleteCredentialAsync(string target) => Task.FromResult(false);
    public Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix) => 
        Task.FromResult(Enumerable.Empty<string>());
}