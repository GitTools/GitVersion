using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.BuildAgents
{
    public class BuildServerModule : GitVersionModule
    {
        public override void RegisterTypes(IServiceCollection services)
        {
            var buildAgents = FindAllDerivedTypes<BuildAgentBase>(Assembly.GetAssembly(GetType()));

            foreach (var buildAgent in buildAgents)
            {
                services.AddSingleton(typeof(IBuildAgent), buildAgent);
            }
        }
    }
}
