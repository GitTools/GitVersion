using GitVersion.BuildServers;
using GitVersion.Cache;
using GitVersion.Configuration;
using GitVersion.Configuration.Init;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IGitVersionCacheKeyFactory, GitVersionCacheKeyFactory>();

            services.AddSingleton<IConfigProvider, ConfigProvider>();
            services.AddSingleton<IVariableProvider, VariableProvider>();

            services.AddSingleton<IMetaDataCalculator, MetaDataCalculator>();
            services.AddSingleton<IBaseVersionCalculator, BaseVersionCalculator>();
            services.AddSingleton<IMainlineVersionCalculator, MainlineVersionCalculator>();
            services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
            services.AddSingleton<IGitVersionCalculator, GitVersionCalculator>();

            services.AddSingleton<IBuildServerResolver, BuildServerResolver>();
            services.AddSingleton<IGitPreparer, GitPreparer>();
            services.AddSingleton<IConfigFileLocatorFactory, ConfigFileLocatorFactory>();

            services.AddSingleton(sp => sp.GetService<IConfigFileLocatorFactory>().Create());

            services.AddModule(new BuildServerModule());
            services.AddModule(new GitVersionInitModule());
            services.AddModule(new VersionStrategyModule());
        }
    }
}
