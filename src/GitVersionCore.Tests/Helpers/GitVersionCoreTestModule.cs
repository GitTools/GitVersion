using GitVersion;
using GitVersion.BuildServers;
using GitVersion.Cache;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests.Helpers
{
    public class GitVersionCoreTestModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddFakeSingleton<IFileSystem>();
            services.AddSingleton<IEnvironment, TestEnvironment>();

            services.AddFakeSingleton<ILog>();
            services.AddFakeSingleton<IConsole>();
            services.AddFakeSingleton<IGitVersionCache>();

            services.AddFakeSingleton<IConfigProvider>();
            services.AddFakeSingleton<IVariableProvider>();
            services.AddFakeSingleton<IGitVersionFinder>();

            services.AddFakeSingleton<IMetaDataCalculator>();
            services.AddFakeSingleton<IBaseVersionCalculator>();
            services.AddFakeSingleton<IMainlineVersionCalculator>();
            services.AddFakeSingleton<INextVersionCalculator>();
            services.AddFakeSingleton<IGitVersionCalculator>();

            services.AddFakeSingleton<IBuildServerResolver>();
            services.AddFakeSingleton<IGitPreparer>();
            services.AddFakeSingleton<IConfigFileLocatorFactory>();

            services.AddSingleton(sp => sp.GetService<IConfigFileLocatorFactory>().Create());

            RegisterBuildServers(services);

            RegisterVersionStrategies(services);
        }

        private static void RegisterBuildServers(IServiceCollection services)
        {
            services.AddFakeSingleton<IBuildServer>();
            services.AddSingleton<AzurePipelines>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
            services.AddFakeSingleton<IBuildServer>();
        }

        private static void RegisterVersionStrategies(IServiceCollection services)
        {
            services.AddFakeSingleton<IVersionStrategy>();
            services.AddFakeSingleton<IVersionStrategy>();
            services.AddFakeSingleton<IVersionStrategy>();
            services.AddFakeSingleton<IVersionStrategy>();
            services.AddFakeSingleton<IVersionStrategy>();
            services.AddFakeSingleton<IVersionStrategy>();
        }
    }
}
