using GitVersion.Infrastructure;

namespace GitVersion;

public class CliModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<GitVersionApp>();
    }
}