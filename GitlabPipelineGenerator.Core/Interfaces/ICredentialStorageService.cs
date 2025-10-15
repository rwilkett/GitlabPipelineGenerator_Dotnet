using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Cross-platform service for secure credential storage and retrieval
/// </summary>
public interface ICredentialStorageService
{
    /// <summary>
    /// Stores credentials securely using the OS credential store
    /// </summary>
    /// <param name="target">Target identifier for the credential</param>
    /// <param name="credentials">Credential data to store</param>
    /// <returns>True if storage was successful, false otherwise</returns>
    Task<bool> StoreCredentialAsync(string target, CredentialData credentials);

    /// <summary>
    /// Retrieves stored credentials from the OS credential store
    /// </summary>
    /// <param name="target">Target identifier for the credential</param>
    /// <returns>Stored credential data, or null if not found</returns>
    Task<CredentialData?> LoadCredentialAsync(string target);

    /// <summary>
    /// Removes stored credentials from the OS credential store
    /// </summary>
    /// <param name="target">Target identifier for the credential</param>
    /// <returns>True if removal was successful, false otherwise</returns>
    Task<bool> DeleteCredentialAsync(string target);

    /// <summary>
    /// Lists all stored credential targets with the specified prefix
    /// </summary>
    /// <param name="targetPrefix">Prefix to filter credential targets</param>
    /// <returns>List of credential target names</returns>
    Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix);

    /// <summary>
    /// Checks if the credential storage service is available on the current platform
    /// </summary>
    /// <returns>True if credential storage is available, false otherwise</returns>
    bool IsAvailable { get; }
}