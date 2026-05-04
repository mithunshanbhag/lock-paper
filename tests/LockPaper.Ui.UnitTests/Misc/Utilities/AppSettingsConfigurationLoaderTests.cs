using LockPaper.Ui.Constants;
using LockPaper.Ui.Misc.Utilities;
using Microsoft.Extensions.Configuration;

namespace LockPaper.Ui.UnitTests.Misc.Utilities;

public sealed class AppSettingsConfigurationLoaderTests
{
    [Fact]
    public void LoadEmbeddedJsonConfiguration_LoadsApplicationInsightsConnectionString()
    {
        var configuration = AppSettingsConfigurationLoader.LoadEmbeddedJsonConfiguration(
            typeof(AppSettingsConfigurationLoaderTests).Assembly,
            "appsettings.json");

        var connectionString = configuration[ConfigKeys.ApplicationInsightsConnectionString];

        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        Assert.Contains("InstrumentationKey=", connectionString, StringComparison.Ordinal);
        Assert.Contains("IngestionEndpoint=", connectionString, StringComparison.Ordinal);
    }
}
