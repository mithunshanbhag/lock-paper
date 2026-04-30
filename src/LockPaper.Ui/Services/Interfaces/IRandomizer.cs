namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Provides app-wide random number generation that can be substituted in tests.
/// </summary>
public interface IRandomizer
{
    /// <summary>
    /// Returns a non-negative random integer that is less than the specified maximum.
    /// </summary>
    /// <param name="maxExclusive">The exclusive upper bound.</param>
    /// <returns>A random integer in the range [0, <paramref name="maxExclusive"/>).</returns>
    int Next(int maxExclusive);
}
