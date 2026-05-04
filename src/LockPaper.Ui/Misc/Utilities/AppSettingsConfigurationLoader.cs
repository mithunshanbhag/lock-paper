using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace LockPaper.Ui.Misc.Utilities;

public static class AppSettingsConfigurationLoader
{
    public static IConfiguration LoadEmbeddedJsonConfiguration(Assembly assembly, string fileName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Could not find embedded configuration resource ending with '{fileName}'.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Could not open embedded configuration resource '{resourceName}'.");

        return new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
    }
}
