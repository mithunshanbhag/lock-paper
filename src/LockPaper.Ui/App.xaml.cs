using Microsoft.Extensions.DependencyInjection;

namespace LockPaper.Ui
{
    public partial class App : Application
    {
#if WINDOWS
        private const double PortraitWindowWidth = 408;
        private const double PortraitWindowHeight = 860;
#endif

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            window.Width = PortraitWindowWidth;
            window.Height = PortraitWindowHeight;
#endif

            return window;
        }
    }
}
