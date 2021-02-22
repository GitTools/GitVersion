using GitVersion.BuildAgents;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.MsBuild
{
    public class GitVersionTaskModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {            
            services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();
        }
    }
}
