using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Windows Credential Manager implementation for credential storage
/// </summary>
internal class WindowsCredentialStorageProvider : ICredentialStorageProvider
{
    private readonly ILogger _logger;

    public WindowsCredentialStorageProvider(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public Task<bool> StoreCredentialAsync(string target, string data)
    {
        try
        {
            if (!IsAvailable)
                return Task.FromResult(false);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var credentialBlobPtr = Marshal.AllocHGlobal(dataBytes.Length);
            Marshal.Copy(dataBytes, 0, credentialBlobPtr, dataBytes.Length);

            var credential = new CREDENTIAL
            {
                TargetName = target,
                Type = CRED_TYPE.GENERIC,
                UserName = Environment.UserName,
                CredentialBlob = credentialBlobPtr,
                CredentialBlobSize = (uint)dataBytes.Length,
                Persist = CRED_PERSIST.LOCAL_MACHINE,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                Comment = "GitLab Pipeline Generator Credentials",
                TargetAlias = null,
                LastWritten = DateTime.UtcNow.ToFileTime()
            };

            try
            {
                var result = CredWrite(ref credential, 0);
                
                if (!result)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to store Windows credential. Error code: {ErrorCode}", error);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            finally
            {
                Marshal.FreeHGlobal(credentialBlobPtr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing Windows credential for target: {Target}", target);
            return Task.FromResult(false);
        }
    }

    public Task<string?> LoadCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
                return Task.FromResult<string?>(null);

            var result = CredRead(target, CRED_TYPE.GENERIC, 0, out var credentialPtr);
            
            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NOT_FOUND)
                {
                    _logger.LogError("Failed to read Windows credential. Error code: {ErrorCode}", error);
                }
                return Task.FromResult<string?>(null);
            }

            try
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                var data = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, data, 0, (int)credential.CredentialBlobSize);
                var credentialData = Encoding.UTF8.GetString(data);
                
                return Task.FromResult<string?>(credentialData);
            }
            finally
            {
                CredFree(credentialPtr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Windows credential for target: {Target}", target);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<bool> DeleteCredentialAsync(string target)
    {
        try
        {
            if (!IsAvailable)
                return Task.FromResult(false);

            var result = CredDelete(target, CRED_TYPE.GENERIC, 0);
            
            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NOT_FOUND)
                {
                    _logger.LogError("Failed to delete Windows credential. Error code: {ErrorCode}", error);
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Windows credential for target: {Target}", target);
            return Task.FromResult(false);
        }
    }

    public Task<IEnumerable<string>> ListCredentialTargetsAsync(string targetPrefix)
    {
        try
        {
            if (!IsAvailable)
                return Task.FromResult(Enumerable.Empty<string>());

            var filter = $"{targetPrefix}*";
            var result = CredEnumerate(filter, 0, out var count, out var credentialsPtr);
            
            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NOT_FOUND)
                {
                    _logger.LogError("Failed to enumerate Windows credentials. Error code: {ErrorCode}", error);
                }
                return Task.FromResult(Enumerable.Empty<string>());
            }

            try
            {
                var targets = new List<string>();
                var credentialPtrs = new IntPtr[count];
                Marshal.Copy(credentialsPtr, credentialPtrs, 0, (int)count);

                foreach (var credPtr in credentialPtrs)
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                    if (!string.IsNullOrEmpty(credential.TargetName))
                    {
                        targets.Add(credential.TargetName);
                    }
                }

                return Task.FromResult<IEnumerable<string>>(targets);
            }
            finally
            {
                CredFree(credentialsPtr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Windows credentials with prefix: {Prefix}", targetPrefix);
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    #region Windows API Declarations

    private const int ERROR_NOT_FOUND = 1168;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, CRED_TYPE type, int flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredEnumerate(string? filter, int flags, out uint count, out IntPtr credentials);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,
        MAXIMUM_EX = (MAXIMUM + 1000)
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }

    #endregion
}