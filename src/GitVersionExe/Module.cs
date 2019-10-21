using GitVersion.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion
{
    public class Module : IModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<IArgumentParser, ArgumentParser>();
            services.AddSingleton<IHelpWriter, HelpWriter>();
            services.AddSingleton<IVersionWriter, VersionWriter>();
            services.AddSingleton<IGitVersionRunner, GitVersionRunner>();

            services.AddTransient<IExecCommand, ExecCommand>();
        }
    }
}
