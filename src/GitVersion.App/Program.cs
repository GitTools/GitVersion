using GitVersion.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class Program
{
    private readonly Action<IServiceCollection> overrides;

    internal Program(Action<IServiceCollection> overrides = null) => this.overrides = overrides;

    private static async Task Main(string[] args) => await new Program().RunAsync(args);

    internal Task RunAsync(string[] args) => CreateHostBuilder(args).Build().RunAsync();

    private IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureAppConfiguration((_, configApp) => configApp.AddCommandLine(args))
            .ConfigureServices((_, services) =>
            {
                services.AddModule(new GitVersionCoreModule());
                services.AddModule(new GitVersionLibGit2SharpModule());
                services.AddModule(new GitVersionAppModule());

                services.AddSingleton(sp =>
                {
                    var arguments = sp.GetService<IArgumentParser>()?.ParseArguments(args);
                    var gitVersionOptions = arguments?.ToOptions();
                    return Options.Create(gitVersionOptions);
                });

                this.overrides?.Invoke(services);
                services.AddHostedService<GitVersionApp>();
            })
            .UseConsoleLifetime();
}
