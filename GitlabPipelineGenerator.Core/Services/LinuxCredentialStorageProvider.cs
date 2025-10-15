using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Linux Secret Service implementation for credential storage
/// </summary>
internal class LinuxCredentialStorageProvider : ICredentialStorageProvider
{
    private readonly ILogger _logger;
    private const string ServiceName = "GitLabPipelineGenerator";

    public LinuxCredentialStorageProvider(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && IsSecretToolAvailable();

    public async Task<bool> StoreCredentialAsync(string target, string data)
    {
        try
        {
            if (!IsAvailable)
                return false;

            // Use secret-tool to store in Secret Service
            var arguments = new[]
            {
                "store",
                "--label", $"{ServiceName} - {target}",
                "service", ServiceName,
                "account", target
            };

            var result = await ExecuteSecretToolCommandAsync(arguments, data);
            
            if (result.ExitCode == 0)
            {
                _logger.LogDebug("Successfully stored credential in Linux Secret Service for target: {Target}", target);
                return true;
            }
            else
            {
                _logger.LogError("Failed to store credential in Linux Secret Service. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing Linux Secret Service credential for target: {Target}", target);
            return false;
        }
    }

    public async Task<string?> LoadCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
                return null;

            // Use secret-tool to retrieve from Secret Service
            var arguments = new[]
            {
                "lookup",
                "service", ServiceName,
                "account", target
            };

            var result = await ExecuteSecretToolCommandAsync(arguments);
            
            if (result.ExitCode == 0)
            {
                var credential = result.StandardOutput.Trim();
                _logger.LogDebug("Successfully loaded credential from Linux Secret Service for target: {Target}", target);
                return credential;
            }
            else if (result.ExitCode == 1) // Item not found
            {
                _logger.LogDebug("No credential found in Linux Secret Service for target: {Target}", target);
                return null;
            }
            else
            {
                _logger.LogError("Failed to load credential from Linux Secret Service. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Linux Secret Service credential for target: {Target}", target);
            return null;
        }
    }

    public async Task<bool> DeleteCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
                return false;

            // Use secret-tool to clear from Secret Service
            var arguments = new[]
            {
                "clear",
                "service", ServiceName,
                "account", target
            };

            var result = await ExecuteSecretToolCommandAsync(arguments);
            
            if (result.ExitCode == 0 || result.ExitCode == 1) // success or item not found
            {
                _logger.LogDebug("Successfully deleted credential from Linux Secret Service for target: {Target}", target);
                return true;
            }
            else
            {
                _logger.LogError("Failed to delete credential from Linux Secret Service. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Linux Secret Service credential for target: {Target}", target);
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix)
    {
        try
        {
            if (!IsAvailable)
                return Enumerable.Empty<string>();

            // Use secret-tool to search for credentials
            var arguments = new[]
            {
                "search",
                "service", ServiceName
            };

            var result = await ExecuteSecretToolCommandAsync(arguments);
            
            if (result.ExitCode != 0)
            {
                _logger.LogError("Failed to list credentials from Linux Secret Service. Exit code: {ExitCode}, Error: {Error}", 
                    result.ExitCode, result.StandardError);
                return Enumerable.Empty<string>();
            }

            // Parse the output to find matching accounts
            var targets = new List<string>();
            var lines = result.StandardOutput.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Look for account attribute lines
                if (trimmedLine.StartsWith("account = "))
                {
                    var account = trimmedLine.Substring("account = ".Length);
                    if (account.StartsWith(targetPrefix))
                    {
                        targets.Add(account);
                    }
                }
            }

            _logger.LogDebug("Found {Count} credential targets with prefix: {Prefix}", targets.Count, targetPrefix);
            return targets.Distinct();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Linux Secret Service credentials with prefix: {Prefix}", targetPrefix);
            return Enumerable.Empty<string>();
        }
    }

    private bool IsSecretToolAvailable()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "secret-tool",
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

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> ExecuteSecretToolCommandAsync(
        string[] arguments, string? input = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "secret-tool",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = input != null,
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

        if (input != null)
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync();

        return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }
}