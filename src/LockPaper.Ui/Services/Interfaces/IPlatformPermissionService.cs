using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Requests any runtime permissions that LockPaper needs once the connected UI is visible.
/// </summary>
public interface IPlatformPermissionService
{
    /// <summary>
    /// Requests the connected-state runtime permissions needed for the current platform.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The result of the permission request for the current platform.</returns>
    Task<PlatformPermissionRequestResult> RequestPostConnectionPermissionsAsync(CancellationToken cancellationToken = default);
}
