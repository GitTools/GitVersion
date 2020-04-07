using GitVersion.MSBuildTask;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion
{
    public class GitVersionTaskModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();
        }
    }
}
