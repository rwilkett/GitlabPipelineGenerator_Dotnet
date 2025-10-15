using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// macOS Keychain implementation for credential storage
/// </summary>
internal class MacOSCredentialStorageProvider : ICredentialStorageProvider
{
    private readonly ILogger _logger;
    private const string ServiceName = "GitLabPipelineGenerator";

    public MacOSCredentialStorageProvider(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && IsSecurityCommandAvailable();

    public async Task<bool> StoreCredentialAsync(string target, string data)
    {
        try
        {
            if (!IsAvailable)
                return false;

            // Use security command to store in keychain
            var arguments = new[]
            {
                "add-generic-password",
                "-a", target,                    // account name
                "-s", ServiceName,               // service name
                "-w", data,                      // password data
                "-U"                             // update if exists
            };

            var result = await ExecuteSecurityCommandAsync(arguments);
            
            if (result.ExitCode == 0)
            {
                _logger.LogDebug("Successfully stored credential in macOS Keychain for target: {Target}", target);
                return true;
            }
            else
            {
                _logger.LogError("Failed to store credential in macOS Keychain. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing macOS Keychain credential for target: {Target}", target);
            return false;
        }
    }

    public async Task<string?> LoadCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
                return null;

            // Use security command to retrieve from keychain
            var arguments = new[]
            {
                "find-generic-password",
                "-a", target,                    // account name
                "-s", ServiceName,               // service name
                "-w"                             // output password only
            };

            var result = await ExecuteSecurityCommandAsync(arguments);
            
            if (result.ExitCode == 0)
            {
                var credential = result.StandardOutput.Trim();
                _logger.LogDebug("Successfully loaded credential from macOS Keychain for target: {Target}", target);
                return credential;
            }
            else if (result.ExitCode == 44) // errSecItemNotFound
            {
                _logger.LogDebug("No credential found in macOS Keychain for target: {Target}", target);
                return null;
            }
            else
            {
                _logger.LogError("Failed to load credential from macOS Keychain. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading macOS Keychain credential for target: {Target}", target);
            return null;
        }
    }

    public async Task<bool> DeleteCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
                return false;

            // Use security command to delete from keychain
            var arguments = new[]
            {
                "delete-generic-password",
                "-a", target,                    // account name
                "-s", ServiceName                // service name
            };

            var result = await ExecuteSecurityCommandAsync(arguments);
            
            if (result.ExitCode == 0 || result.ExitCode == 44) // success or item not found
            {
                _logger.LogDebug("Successfully deleted credential from macOS Keychain for target: {Target}", target);
                return true;
            }
            else
            {
                _logger.LogError("Failed to delete credential from macOS Keychain. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting macOS Keychain credential for target: {Target}", target);
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix)
    {
        try
        {
            if (!IsAvailable)
                return Enumerable.Empty<string>();

            // Use security command to dump keychain items
            var arguments = new[]
            {
                "dump-keychain",
                "-d"                             // include decrypted data
            };

            var result = await ExecuteSecurityCommandAsync(arguments);
            
            if (result.ExitCode != 0)
            {
                _logger.LogError("Failed to list credentials from macOS Keychain. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return Enumerable.Empty<string>();
            }

            // Parse the output to find matching service entries
            var targets = new List<string>();
            var lines = result.StandardOutput.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Look for service name match
                if (line.Contains($"\"svce\"<blob>=\"{ServiceName}\""))
                {
                    // Look for account name in nearby lines
                    for (int j = Math.Max(0, i - 5); j < Math.Min(lines.Length, i + 5); j++)
                    {
                        var accountLine = lines[j].Trim();
                        if (accountLine.StartsWith("\"acct\"<blob>="))
                        {
                            var accountMatch = System.Text.RegularExpressions.Regex.Match(
                                accountLine, "\"acct\"<blob>=\"([^\"]+)\"");
                            
                            if (accountMatch.Success)
                            {
                                var account = accountMatch.Groups[1].Value;
                                if (account.StartsWith(targetPrefix))
                                {
                                    targets.Add(account);
                                }
                            }
                            break;
                        }
                    }
                }
            }

            _logger.LogDebug("Found {Count} credential targets with prefix: {Prefix}", targets.Count, targetPrefix);
            return targets.Distinct();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing macOS Keychain credentials with prefix: {Prefix}", targetPrefix);
            return Enumerable.Empty<string>();
        }
    }

    private bool IsSecurityCommandAvailable()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "security",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> ExecuteSecurityCommandAsync(string[] arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }
}