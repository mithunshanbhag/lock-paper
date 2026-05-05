using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LockPaper.Ui.Misc.Telemetry;

namespace LockPaper.Ui
{
    public partial class App : Application
    {
#if WINDOWS
        private const double PortraitWindowWidth = 408;
        private const double PortraitWindowHeight = 860;
#endif

        private readonly ILogger<App> _logger;
        private readonly TelemetryClient _telemetryClient;

        public App(ILogger<App> logger, TelemetryClient telemetryClient)
        {
            InitializeComponent();
            _logger = logger;
            _telemetryClient = telemetryClient;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _logger.LogInformation("LockPaper app initialized.");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var checkpoint = PerformanceCheckpoint.StartNew("App.CreateWindow");
            var outcome = "Succeeded";

            try
            {
                _logger.LogInformation("Creating main application window.");
                var window = new Window(new AppShell());

#if WINDOWS
                window.Width = PortraitWindowWidth;
                window.Height = PortraitWindowHeight;
#endif

                return window;
            }
            catch (Exception)
            {
                outcome = "Failed";
                throw;
            }
            finally
            {
                checkpoint.LogCompleted(_logger, outcome);
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger.LogCritical(exception, "Unhandled application exception. Is terminating: {IsTerminating}", e.IsTerminating);
                FlushTelemetry();
                return;
            }

            _logger.LogCritical("Unhandled non-exception application failure. Is terminating: {IsTerminating}. Payload: {Payload}", e.IsTerminating, e.ExceptionObject);
            FlushTelemetry();
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogCritical(e.Exception, "Unobserved task exception reached the application root.");
            FlushTelemetry();
        }

        private void FlushTelemetry()
        {
            try
            {
                _telemetryClient.Flush();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Flushing Application Insights telemetry failed.");
            }
        }
    }
}
