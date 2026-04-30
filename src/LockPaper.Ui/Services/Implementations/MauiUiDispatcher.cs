using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui.Services.Implementations;

public sealed class MauiUiDispatcher(ILogger<MauiUiDispatcher> logger) : IUiDispatcher
{
    public async Task DispatchAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (MainThread.IsMainThread)
        {
            logger.LogDebug("Executing dispatched action on the current UI thread.");

            try
            {
                action();
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Executing a dispatched action on the current UI thread failed.");
                throw;
            }
        }

        logger.LogDebug("Dispatching action onto the UI thread.");

        try
        {
            await MainThread.InvokeOnMainThreadAsync(action);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Executing a dispatched action on the UI thread failed.");
            throw;
        }
    }
}
