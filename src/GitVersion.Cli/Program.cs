using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace GitVersion.Cli
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            await new Program().RunAsync(args);
        }

        internal Task RunAsync(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }

        private IHostBuilder CreateHostBuilder(string[] args) =>
           new HostBuilder()
               .ConfigureAppConfiguration((hostContext, configApp) =>
               {
                   configApp.AddCommandLine(args);
               })
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddModule(new GitVersionCoreModule());
                   services.AddSingleton<IGitVersionCalculator, GitVersionCalculator>();

                   services.AddSingleton<CommandWrapper>();

                   services.AddSingleton<RootCommand>();
                   services.AddSingleton<CalculateCommand>();

                   services.AddHostedService<HostedService>(sp =>
                   {
                       var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
                       var rootCommand = sp.GetRequiredService<RootCommand>();
                       var log = sp.GetRequiredService<ILog>();
                       var hostedService = new HostedService(lifetime, rootCommand, log, args);
                       return hostedService;
                       // return ActivatorUtilities.CreateInstance<HostedService>(sp, args);      
                   });
               })
               .UseConsoleLifetime();
    }

}
