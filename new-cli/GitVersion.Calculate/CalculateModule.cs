using GitVersion.Core.Infrastructure;

namespace GitVersion.Calculate
{
    public class CalculateModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<IRootCommandHandler, CalculateCommandHandler>();
        }
    }
}