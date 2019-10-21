using GitVersion.BuildServers;
using Microsoft.Extensions.DependencyInjection;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersion
{
    public class CoreModule : IModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IEnvironment, Environment>();
            services.AddSingleton<ILog, Log>();

            services.AddSingleton<IExecuteCore, ExecuteCore>();

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

            services.AddSingleton<IBuildServerResolver, BuildServerResolver>();
        }
    }
}
