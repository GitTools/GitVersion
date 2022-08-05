using GitVersion.AssemblyInfo;
using GitVersion.Infrastructure;
using GitVersion.Project;
using GitVersion.Wix;

namespace GitVersion;

public class OutputModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<OutputCommand>();
        services.AddSingleton<OutputAssemblyInfoCommand>();
        services.AddSingleton<OutputProjectCommand>();
        services.AddSingleton<OutputWixCommand>();
    }
}
