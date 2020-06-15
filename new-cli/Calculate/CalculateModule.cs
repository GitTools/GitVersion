using Core;
using Microsoft.Extensions.DependencyInjection;

namespace Calculate
{
    public class CalculateModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<ICommandHandler, CalculateCommandHandler>();
        }
    }
}