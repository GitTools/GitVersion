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
            services.AddSingleton<IRootCommandHandler, OutputCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputAssemblyInfoCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputProjectCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputWixCommandHandler>();
        }
    }
}