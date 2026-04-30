namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Dispatches work onto the UI thread when a view model needs to update UI-bound state.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// Runs the supplied action on the UI thread.
    /// </summary>
    /// <param name="action">The UI-bound action to run.</param>
    /// <returns>A task that completes after the action has run.</returns>
    Task DispatchAsync(Action action);
}
