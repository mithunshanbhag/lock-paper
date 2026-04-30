namespace LockPaper.Ui.Services.Implementations;

public sealed class SystemRandomizer : IRandomizer
{
    public int Next(int maxExclusive) => Random.Shared.Next(maxExclusive);
}
