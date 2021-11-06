using GitVersion.Command;
using GitVersion.Infrastructure;
using GitVersion.Output.AssemblyInfo;
using GitVersion.Output.Project;
using GitVersion.Output.Wix;

namespace GitVersion.Output;

public class OutputModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<ICommand, OutputCommand>();
        services.AddSingleton<ICommand, OutputAssemblyInfoCommand>();
        services.AddSingleton<ICommand, OutputProjectCommand>();
        services.AddSingleton<ICommand, OutputWixCommand>();
    }
}