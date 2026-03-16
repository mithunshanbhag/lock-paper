var host = WebAssemblyHostBuilder
    .CreateDefault(args)
    .ConfigureApp()
    .ConfigureServices()
    .Build();

await host.RunAsync();