using GitVersion.AssemblyInfo;
using GitVersion.Infrastructure;
using GitVersion.Project;
using GitVersion.Wix;

namespace GitVersion;

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