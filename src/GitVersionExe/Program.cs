using System;
using System.Diagnostics;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Console = System.Console;
using Environment = GitVersion.Common.Environment;

namespace GitVersion
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var arguments = ParseArguments(args);

            var exitCode = 1;
            if (arguments != null)
            {
                var services = ConfigureServices(arguments);

                using (var serviceProvider = services.BuildServiceProvider())
                {
                    try
                    {
                        var app = serviceProvider.GetService<IGitVersionRunner>();
                        exitCode = app.Run(arguments);
                    }
                    catch (Exception exception)
                    {
                        Console.Error.WriteLine(exception.Message);
                    }
                }
            }

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            System.Environment.Exit(exitCode);
        }

        private static IServiceCollection ConfigureServices(Arguments arguments)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IEnvironment, Environment>();
            services.AddSingleton<IHelpWriter, HelpWriter>();
            services.AddSingleton<IVersionWriter, VersionWriter>();
            services.AddSingleton<ILog>(new Log { Verbosity = arguments.Verbosity });
            services.AddSingleton(sp => ConfigFileLocator(sp, arguments));
            services.AddSingleton<IGitVersionRunner, GitVersionRunner>();

            return services;
        }

        private static IConfigFileLocator ConfigFileLocator(IServiceProvider sp, Arguments arguments)
        {
            var fileSystem = sp.GetService<IFileSystem>();
            var log = sp.GetService<ILog>();

            var configFileLocator = string.IsNullOrWhiteSpace(arguments.ConfigFile)
                ? (IConfigFileLocator) new DefaultConfigFileLocator(fileSystem, log)
                : new NamedConfigFileLocator(arguments.ConfigFile, fileSystem, log);

            return configFileLocator;
        }

        private static Arguments ParseArguments(string[] args)
        {
            var argumentParser = new ArgumentParser();
            Arguments arguments = null;
            try
            {
                arguments = argumentParser.ParseArguments(args);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            return arguments;
        }
    }
}
