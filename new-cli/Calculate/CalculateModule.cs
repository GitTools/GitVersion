using Core;

namespace Calculate
{
    public class CalculateModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<ICommandHandler, CalculateCommandHandler>();
        }
    }
}