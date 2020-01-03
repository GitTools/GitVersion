using GitVersion.BuildServers;
using GitVersion.Cache;
using Microsoft.Extensions.DependencyInjection;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
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

            services.AddSingleton<IConfigProvider, ConfigProvider>();
            services.AddSingleton<IVariableProvider, VariableProvider>();
            services.AddSingleton<IGitVersionFinder, GitVersionFinder>();

            services.AddSingleton<IMetaDataCalculator, MetaDataCalculator>();
            services.AddSingleton<IBaseVersionCalculator, BaseVersionCalculator>();
            services.AddSingleton<IMainlineVersionCalculator, MainlineVersionCalculator>();
            services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
            services.AddSingleton<IGitVersionCalculator, GitVersionCalculator>();

            services.AddSingleton<IBuildServerResolver, BuildServerResolver>();
            services.AddSingleton<IGitPreparer, GitPreparer>();
            services.AddSingleton<IConfigFileLocatorFactory, ConfigFileLocatorFactory>();

            services.AddSingleton(sp => sp.GetService<IConfigFileLocatorFactory>().Create());

            RegisterBuildServers(services);

            RegisterVersionStrategies(services);

            services.AddModule(new GitVersionInitModule());
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
            services.AddSingleton<IBuildServer, TravisCi>();
            services.AddSingleton<IBuildServer, EnvRun>();
            services.AddSingleton<IBuildServer, Drone>();
            services.AddSingleton<IBuildServer, CodeBuild>();
            services.AddSingleton<IBuildServer, GitHubActions>();
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
