using GitVersion.Infrastructure;

namespace GitVersion;

public class TesterModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services) => services.AddSingleton<GitVersionApp>();
}
