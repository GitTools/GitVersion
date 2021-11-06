namespace GitVersion.Infrastructure;

public interface IGitVersionModule
{
    void RegisterTypes(IContainerRegistrar services);
}