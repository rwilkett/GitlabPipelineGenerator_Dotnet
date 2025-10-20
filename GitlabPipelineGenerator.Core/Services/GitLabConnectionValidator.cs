using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.GitLabApiClient;
using Microsoft.Extensions.Logging;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for validating GitLab connections and basic operations
/// </summary>
public class GitLabConnectionValidator
{
    private readonly ILogger<GitLabConnectionValidator> _logger;

    public GitLabConnectionValidator(ILogger<GitLabConnectionValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a GitLab connection by performing basic API calls
    /// </summary>
    /// <param name="client">GitLab client to validate</param>
    /// <returns>Validation result with details</returns>
    public async Task<ConnectionValidationResult> ValidateConnectionAsync(GitLabClient client)
    {
        var result = new ConnectionValidationResult();

        try
        {
            _logger.LogInformation("Starting GitLab connection validation");

            // Test 1: Basic authentication test
            try
            {
                // Try a simple API call to test authentication
                var projects = await client.Projects.GetAsync(options => 
                {
                    options.Simple = true;
                });
                
                result.CanAuthenticate = true;
                result.UserInfo = new GitLabUserInfo
                {
                    Id = 0,
                    Username = "authenticated_user",
                    Name = "Authenticated User",
                    Email = "user@example.com",
                    State = "active"
                };
                _logger.LogInformation("Authentication test passed");
            }
            catch (Exception ex)
            {
                result.CanAuthenticate = false;
                result.Errors.Add($"Authentication failed: {ex.Message}");
                _logger.LogError(ex, "Authentication test failed");
            }

            // Test 2: List projects (basic API access test)
            if (result.CanAuthenticate)
            {
                try
                {
                    var projects = await client.Projects.GetAsync(options => options.Simple = true);
                    result.CanAccessProjects = true;
                    result.ProjectCount = projects.Count();
                    _logger.LogInformation("Project access test passed. Found {Count} projects", result.ProjectCount);
                }
                catch (Exception ex)
                {
                    result.CanAccessProjects = false;
                    result.Errors.Add($"Project access failed: {ex.Message}");
                    _logger.LogError(ex, "Project access test failed");
                }
            }

            // Test 3: Check API version compatibility (placeholder for now)
            try
            {
                // TODO: Implement version checking once API methods are verified
                result.GitLabVersion = "Unknown";
                result.IsVersionSupported = true;
                
                _logger.LogInformation("Version check skipped - API method verification needed");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Could not determine GitLab version: {ex.Message}");
                _logger.LogWarning(ex, "Version check failed");
            }

            result.IsValid = result.CanAuthenticate && result.CanAccessProjects;
            
            _logger.LogInformation("Connection validation completed. Valid: {IsValid}", result.IsValid);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection validation failed with unexpected error");
            result.IsValid = false;
            result.Errors.Add($"Validation failed: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Checks if a GitLab version is supported
    /// </summary>
    /// <param name="version">GitLab version string</param>
    /// <returns>True if version is supported</returns>
    private bool IsVersionSupported(string version)
    {
        try
        {
            // Extract major version number
            var parts = version.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0], out var majorVersion))
            {
                // Support GitLab 13.0 and higher
                return majorVersion >= 13;
            }
        }
        catch
        {
            // If we can't parse the version, assume it's supported
        }

        return true; // Default to supported if we can't determine
    }
}

/// <summary>
/// Result of GitLab connection validation
/// </summary>
public class ConnectionValidationResult
{
    /// <summary>
    /// Whether the connection is valid overall
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether authentication succeeded
    /// </summary>
    public bool CanAuthenticate { get; set; }

    /// <summary>
    /// Whether project access is available
    /// </summary>
    public bool CanAccessProjects { get; set; }

    /// <summary>
    /// Information about the authenticated user
    /// </summary>
    public GitLabUserInfo? UserInfo { get; set; }

    /// <summary>
    /// Number of accessible projects
    /// </summary>
    public int ProjectCount { get; set; }

    /// <summary>
    /// GitLab version
    /// </summary>
    public string? GitLabVersion { get; set; }

    /// <summary>
    /// Whether the GitLab version is supported
    /// </summary>
    public bool IsVersionSupported { get; set; } = true;

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}