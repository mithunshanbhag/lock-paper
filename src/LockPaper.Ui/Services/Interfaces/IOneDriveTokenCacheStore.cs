namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Persists the serialized MSAL user token cache for OneDrive sign-in.
/// </summary>
public interface IOneDriveTokenCacheStore
{
    /// <summary>
    /// Reads the serialized token cache, if one has been saved for this device.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The serialized cache bytes, or <see langword="null" /> when there is no saved cache.</returns>
    Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the serialized token cache for future app launches.
    /// </summary>
    /// <param name="cacheBytes">The serialized MSAL token cache bytes.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    Task WriteAsync(byte[] cacheBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the saved token cache for this device.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
