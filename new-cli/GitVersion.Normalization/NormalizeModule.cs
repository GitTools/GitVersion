using GitVersion.Infrastructure;

namespace GitVersion.Normalization
{
    public class NormalizeModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<IRootCommandHandler, NormalizeCommandHandler>();
        }
    }
}