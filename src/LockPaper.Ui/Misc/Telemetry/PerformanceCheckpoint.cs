using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LockPaper.Ui.Misc.Telemetry;

internal sealed class PerformanceCheckpoint
{
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    private PerformanceCheckpoint(string operationName)
    {
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public static PerformanceCheckpoint StartNew(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        return new PerformanceCheckpoint(operationName);
    }

    public void LogCompleted(ILogger logger, string outcome)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _stopwatch.Stop();

        logger.LogInformation(
            "Performance checkpoint {OperationName} completed with outcome {Outcome} in {ElapsedMs} ms.",
            _operationName,
            string.IsNullOrWhiteSpace(outcome) ? "Unknown" : outcome,
            Math.Round(_stopwatch.Elapsed.TotalMilliseconds, 2));
    }
}
