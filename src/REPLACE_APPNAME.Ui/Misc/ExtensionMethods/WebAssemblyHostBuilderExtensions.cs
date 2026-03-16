namespace REPLACE_APPNAME.Ui.Misc.ExtensionMethods;

public static class WebAssemblyHostBuilderExtensions
{
    extension(WebAssemblyHostBuilder builder)
    {
        public WebAssemblyHostBuilder ConfigureApp()
        {
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            return builder;
        }

        public WebAssemblyHostBuilder ConfigureServices()
        {
            // automapper
            builder.Services.AddAutoMapper(cfg => { cfg.AddProfile<MapperProfile>(); });

            // mediatr
            builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()); });

            // named http clients

            // mudblazor
            builder.Services.AddMudServices();

            // auth

            // services
            builder.Services
                .AddTransient<IService1, Service1>();

            // repositories
            builder.Services
                .AddTransient<ICsvRepository1, CsvRepository1>();

            return builder;
        }
    }
}