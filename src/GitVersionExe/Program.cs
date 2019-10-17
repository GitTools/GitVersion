using System;
using System.Threading.Tasks;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Environment = GitVersion.Common.Environment;

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
                    services.AddSingleton<IArgumentParser, ArgumentParser>();
                    services.AddSingleton<IFileSystem, FileSystem>();
                    services.AddSingleton<IEnvironment, Environment>();
                    services.AddSingleton<IHelpWriter, HelpWriter>();
                    services.AddSingleton<IVersionWriter, VersionWriter>();
                    services.AddSingleton<IGitVersionRunner, GitVersionRunner>();

                    services.AddSingleton(GetLog);
                    services.AddSingleton(GetConfigFileLocator);
                    services.AddSingleton(sp => GetArguments(sp, args));

                    services.AddHostedService<GitVersionApp>();
                })
                .UseConsoleLifetime();

        private static ILog GetLog(IServiceProvider sp)
        {
            var arguments = sp.GetService<IOptions<Arguments>>();
            return new Log { Verbosity = arguments.Value.Verbosity };
        }

        private static IOptions<Arguments> GetArguments(IServiceProvider sp, string[] args)
        {
            var argumentParser = sp.GetService<IArgumentParser>();
            var arguments = argumentParser.ParseArguments(args);

            return Options.Create(arguments);
        }

        private static IConfigFileLocator GetConfigFileLocator(IServiceProvider sp)
        {
            var fileSystem = sp.GetService<IFileSystem>();
            var log = sp.GetService<ILog>();
            var arguments = sp.GetService<IOptions<Arguments>>();

            var configFileLocator = string.IsNullOrWhiteSpace(arguments.Value.ConfigFile)
                ? (IConfigFileLocator) new DefaultConfigFileLocator(fileSystem, log)
                : new NamedConfigFileLocator(arguments.Value.ConfigFile, fileSystem, log);

            return configFileLocator;
        }
    }
}
