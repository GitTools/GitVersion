using GitVersion.Command;
using GitVersion.Infrastructure;
using GitVersion.Output.AssemblyInfo;
using GitVersion.Output.Project;
using GitVersion.Output.Wix;

namespace GitVersion.Output
{
    public class OutputModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<ICommandHandler, OutputCommandHandler>();
            services.AddSingleton<ICommandHandler, OutputAssemblyInfoCommandHandler>();
            services.AddSingleton<ICommandHandler, OutputProjectCommandHandler>();
            services.AddSingleton<ICommandHandler, OutputWixCommandHandler>();
        }
    }
}