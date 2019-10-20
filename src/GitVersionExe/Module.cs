using System;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class Module : IModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IArgumentParser, ArgumentParser>();
            services.AddSingleton<IHelpWriter, HelpWriter>();
            services.AddSingleton<IVersionWriter, VersionWriter>();
            services.AddSingleton<IGitVersionRunner, GitVersionRunner>();

            services.AddSingleton(GetConfigFileLocator);
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
