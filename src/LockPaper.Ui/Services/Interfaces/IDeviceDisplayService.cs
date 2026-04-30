using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Reads the current device display or monitor configuration for the local device.
/// </summary>
public interface IDeviceDisplayService
{
    /// <summary>
    /// Gets the displays that are currently attached to the device.
    /// </summary>
    /// <returns>A snapshot of the currently attached displays.</returns>
    IReadOnlyList<DeviceDisplayInfo> GetDisplays();
}
