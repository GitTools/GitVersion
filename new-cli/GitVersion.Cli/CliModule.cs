using GitVersion.Infrastructure;

namespace GitVersion.Cli
{
    public class CliModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<GitVersionApp>();
        }
    }
}