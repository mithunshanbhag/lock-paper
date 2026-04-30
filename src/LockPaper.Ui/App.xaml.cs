using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui
{
    public partial class App : Application
    {
#if WINDOWS
        private const double PortraitWindowWidth = 408;
        private const double PortraitWindowHeight = 860;
#endif

        private readonly ILogger<App> _logger;

        public App(ILogger<App> logger)
        {
            InitializeComponent();
            _logger = logger;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _logger.LogInformation("LockPaper app initialized.");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _logger.LogInformation("Creating main application window.");
            var window = new Window(new AppShell());

#if WINDOWS
            window.Width = PortraitWindowWidth;
            window.Height = PortraitWindowHeight;
#endif

            return window;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger.LogCritical(exception, "Unhandled application exception. Is terminating: {IsTerminating}", e.IsTerminating);
                return;
            }

            _logger.LogCritical("Unhandled non-exception application failure. Is terminating: {IsTerminating}. Payload: {Payload}", e.IsTerminating, e.ExceptionObject);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogCritical(e.Exception, "Unobserved task exception reached the application root.");
        }
    }
}
