using System.Threading.Tasks;
using GitVersion.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddModule(new GitVersionCoreModule());
                    services.AddModule(new GitVersionExeModule());

                    services.AddSingleton(sp => Options.Create(sp.GetService<IArgumentParser>().ParseArguments(args)));

                    services.AddHostedService<GitVersionApp>();
                })
                .UseConsoleLifetime();
    }
}
