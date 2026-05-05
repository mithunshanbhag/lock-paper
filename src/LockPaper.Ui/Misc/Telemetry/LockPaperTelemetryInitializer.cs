using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace LockPaper.Ui.Misc.Telemetry;

public sealed class LockPaperTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        try
        {
            telemetry.Context.Component.Version ??= $"{AppInfo.VersionString} ({AppInfo.BuildString})";
            telemetry.Context.Device.Type ??= DeviceInfo.Platform.ToString();
            telemetry.Context.Device.OperatingSystem ??= $"{DeviceInfo.Platform} {DeviceInfo.VersionString}";

            AddPropertyIfMissing(telemetry, "LockPaper.Platform", DeviceInfo.Platform.ToString());
            AddPropertyIfMissing(telemetry, "LockPaper.DeviceModel", DeviceInfo.Model);
            AddPropertyIfMissing(telemetry, "LockPaper.Manufacturer", DeviceInfo.Manufacturer);
            AddPropertyIfMissing(telemetry, "LockPaper.Version", AppInfo.VersionString);
            AddPropertyIfMissing(telemetry, "LockPaper.Build", AppInfo.BuildString);
        }
        catch
        {
            // Telemetry enrichment must never break the app or telemetry pipeline.
        }
    }

    private static void AddPropertyIfMissing(ITelemetry telemetry, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!telemetry.Context.GlobalProperties.ContainsKey(key))
        {
            telemetry.Context.GlobalProperties[key] = value;
        }
    }
}
