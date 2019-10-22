using System;
using GitVersion.BuildServers;
using Microsoft.Extensions.DependencyInjection;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using Microsoft.Extensions.Options;
using Environment = GitVersion.Common.Environment;

namespace GitVersion
{
    public class GitVersionModule : IModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IEnvironment, Environment>();
            services.AddSingleton<ILog, Log>();

            services.AddSingleton<IGitVersionComputer, GitVersionComputer>();

            services.AddSingleton<IBuildServerResolver, BuildServerResolver>();
            services.AddSingleton(GetConfigFileLocator);

            RegisterBuildServers(services);
        }

        private static IConfigFileLocator GetConfigFileLocator(IServiceProvider sp)
        {
            var fileSystem = sp.GetService<IFileSystem>();
            var log = sp.GetService<ILog>();
            var arguments = sp.GetService<IOptions<Arguments>>();

            var configFile = arguments.Value.ConfigFile;

            var configFileLocator = string.IsNullOrWhiteSpace(configFile)
                ? new DefaultConfigFileLocator(fileSystem, log) as IConfigFileLocator
                : new NamedConfigFileLocator(configFile, fileSystem, log);

            return configFileLocator;
        }

        private static void RegisterBuildServers(IServiceCollection services)
        {
            services.AddSingleton<IBuildServer, ContinuaCi>();
            services.AddSingleton<IBuildServer, TeamCity>();
            services.AddSingleton<IBuildServer, AppVeyor>();
            services.AddSingleton<IBuildServer, MyGet>();
            services.AddSingleton<IBuildServer, Jenkins>();
            services.AddSingleton<IBuildServer, GitLabCi>();
            services.AddSingleton<IBuildServer, AzurePipelines>();
            services.AddSingleton<IBuildServer, TravisCI>();
            services.AddSingleton<IBuildServer, EnvRun>();
            services.AddSingleton<IBuildServer, Drone>();
            services.AddSingleton<IBuildServer, CodeBuild>();
        }
    }
}
