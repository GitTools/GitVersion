using GitVersion.Infrastructure;

namespace GitVersion
{
    public class CalculateModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services) => services.AddSingleton<ICommand, CalculateCommand>();
    }
}
