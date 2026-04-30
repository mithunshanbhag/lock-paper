using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Handles the interactive OneDrive authentication lifecycle for the current device.
/// </summary>
public interface IOneDriveAuthenticationService
{
    /// <summary>
    /// Gets the current OneDrive connection state for the device.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The current connection state, including whether a reconnect is required.</returns>
    Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an interactive OneDrive sign-in flow.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The result of the sign-in attempt and the resulting connection state.</returns>
    Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Microsoft Graph access token for the signed-in OneDrive account.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The access token for Microsoft Graph requests.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current device does not have a usable signed-in OneDrive session.</exception>
    Task<string> GetMicrosoftGraphAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears any cached OneDrive account session for the device.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The result of the sign-out attempt and the resulting connection state.</returns>
    Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default);
}
