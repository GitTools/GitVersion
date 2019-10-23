using Microsoft.Extensions.DependencyInjection;

namespace GitVersion
{
    public class GitVersionExeModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IArgumentParser, ArgumentParser>();
            services.AddSingleton<IHelpWriter, HelpWriter>();
            services.AddSingleton<IVersionWriter, VersionWriter>();
            services.AddSingleton<IGitVersionExecutor, GitVersionExecutor>();

            services.AddTransient<IExecCommand, ExecCommand>();
        }
    }
}
