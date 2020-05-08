using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
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
        private readonly Action<IServiceCollection> overrides;

        internal Program(Action<IServiceCollection> overrides = null)
        {
            this.overrides = overrides;
        }

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
                    services.AddModule(new GitVersionExeModule());

                    // return Options.Create(gitVersionOptions);

                    //services.AddSingleton(sp =>
                    //{
                    //    var arguments = sp.GetService<IArgumentParser>().ParseArguments(args);
                    //    var gitVersionOptions = arguments.ToOptions();

                    //});                
                    services.AddOptions<GitVersionOptions>()
                            .PostConfigure(a => a.Args = args);

                    services.AddSingleton<GitVersionRootCommand>();
                    services.AddSingleton<CalculateCommand>();
                    //services.AddSingleton(sp =>
                    //{                       
                    //    return BuildCommand(sp);
                    //});

                    overrides?.Invoke(services);
                    services.AddHostedService<GitVersionApp>();
                })
                .UseConsoleLifetime();

        //private static RootCommand BuildCommand(IServiceProvider serviceProvider)
        //{
        //    var rootCommand = ActivatorUtilities.GetServiceOrCreateInstance<GitVersionRootCommand>(serviceProvider);
        //    return rootCommand;
        //}
    }
}
