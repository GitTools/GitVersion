using Core;

namespace Output
{
    public class OutputModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<ICommandHandler, OutputCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputAssemblyInfoCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputProjectCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputWixCommandHandler>();
        }
    }
}