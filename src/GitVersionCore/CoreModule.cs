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
        }
    }
}
