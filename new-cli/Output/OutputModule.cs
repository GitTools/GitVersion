using System.CommandLine;
using Core;
using Microsoft.Extensions.DependencyInjection;

namespace Output
{
    public class OutputModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddSingleton<Command, OutputCommand>();
            services.AddSingleton<BaseOutputCommand, OutputAssemblyInfoCommand>();
            services.AddSingleton<BaseOutputCommand, OutputProjectCommand>();
        }
    }
}