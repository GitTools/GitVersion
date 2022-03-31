using GitVersion.Infrastructure;

namespace GitVersion;

public class NormalizeModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services) => services.AddSingleton<ICommand, NormalizeCommand>();
}
