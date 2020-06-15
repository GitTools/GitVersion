using Core;
using Microsoft.Extensions.DependencyInjection;

namespace Output
{
    public class OutputModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<ICommandHandler, OutputCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputAssemblyInfoCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputProjectCommandHandler>();
            services.AddSingleton<IOutputCommandHandler, OutputWixCommandHandler>();
        }
    }
}