namespace GitVersion.Core.Infrastructure
{
    public interface IGitVersionModule
    {
        void RegisterTypes(IContainerRegistrar services);
    }
}