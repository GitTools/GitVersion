using GitVersion.Core.Infrastructure;

namespace GitVersion.Normalize
{
    public class NormalizeModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<IRootCommandHandler, NormalizeCommandHandler>();
        }
    }
}