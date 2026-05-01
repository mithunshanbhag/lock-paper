using Microsoft.Maui.Storage;
#if WINDOWS
using System.Security.Cryptography;
#endif

namespace LockPaper.Ui.Services.Implementations;

public sealed class SecureOneDriveTokenCacheStore : IOneDriveTokenCacheStore
{
    private const string TokenCacheKey = "LockPaper.OneDrive.Msal.UserTokenCache.v1";
    private const string TokenCacheFileName = "msal-onedrive-token-cache.bin3";

    public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if WINDOWS
        if (!File.Exists(TokenCacheFilePath))
        {
            return null;
        }

        var protectedCacheBytes = await File.ReadAllBytesAsync(TokenCacheFilePath, cancellationToken).ConfigureAwait(false);
        if (protectedCacheBytes.Length == 0)
        {
            return null;
        }

        return ProtectedData.Unprotect(protectedCacheBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
#else
        var cacheValue = await SecureStorage.Default.GetAsync(TokenCacheKey).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheValue))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(cacheValue);
        }
        catch (FormatException)
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
#endif
    }

    public async Task WriteAsync(byte[] cacheBytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (cacheBytes.Length == 0)
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

#if WINDOWS
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        var protectedCacheBytes = ProtectedData.Protect(cacheBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(TokenCacheFilePath, protectedCacheBytes, cancellationToken).ConfigureAwait(false);
#else
        await SecureStorage.Default
            .SetAsync(TokenCacheKey, Convert.ToBase64String(cacheBytes))
            .ConfigureAwait(false);
#endif
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if WINDOWS
        if (File.Exists(TokenCacheFilePath))
        {
            File.Delete(TokenCacheFilePath);
        }
#else
        SecureStorage.Default.Remove(TokenCacheKey);
#endif
        return Task.CompletedTask;
    }

#if WINDOWS
    private static string TokenCacheFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, TokenCacheFileName);
#endif
}
