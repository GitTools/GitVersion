using System;
using GitVersion.BuildServers;
using GitVersion.Cache;
using Microsoft.Extensions.DependencyInjection;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using Microsoft.Extensions.Options;
using GitVersion.Configuration.Init;
using GitVersion.Extensions;

namespace GitVersion
{
    public class GitVersionCoreModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IEnvironment, Environment>();
            services.AddSingleton<ILog, Log>();
            services.AddSingleton<IConsole, ConsoleAdapter>();
            services.AddSingleton<IGitVersionCache, GitVersionCache>();

            services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
            services.AddSingleton<IVariableProvider, VariableProvider>();
            services.AddSingleton<IGitVersionFinder, GitVersionFinder>();

            services.AddSingleton<IMetaDataCalculator, MetaDataCalculator>();
            services.AddSingleton<IBaseVersionCalculator, BaseVersionCalculator>();
            services.AddSingleton<IMainlineVersionCalculator, MainlineVersionCalculator>();
            services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
            services.AddSingleton<IGitVersionCalculator, GitVersionCalculator>();

            services.AddSingleton<IBuildServerResolver, BuildServerResolver>();
            services.AddSingleton<IGitPreparer, GitPreparer>();

            services.AddSingleton(GetConfigFileLocator);

            RegisterBuildServers(services);

            RegisterVersionStrategies(services);

            services.AddModule(new GitVersionInitModule());
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

        private static void RegisterVersionStrategies(IServiceCollection services)
        {
            services.AddSingleton<IVersionStrategy, FallbackVersionStrategy>();
            services.AddSingleton<IVersionStrategy, ConfigNextVersionVersionStrategy>();
            services.AddSingleton<IVersionStrategy, TaggedCommitVersionStrategy>();
            services.AddSingleton<IVersionStrategy, MergeMessageVersionStrategy>();
            services.AddSingleton<IVersionStrategy, VersionInBranchNameVersionStrategy>();
            services.AddSingleton<IVersionStrategy, TrackReleaseBranchesVersionStrategy>();
        }
    }
}
