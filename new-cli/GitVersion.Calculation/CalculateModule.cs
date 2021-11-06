using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Calculation
{
    public class CalculateModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<ICommand, CalculateCommand>();
        }
    }
}