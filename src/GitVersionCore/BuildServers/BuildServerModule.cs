using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.BuildServers
{
    public class BuildServerModule : GitVersionModule
    {
        public override void RegisterTypes(IServiceCollection services)
        {
            var buildServers = FindAllDerivedTypes<BuildServerBase>(Assembly.GetAssembly(GetType()));

            foreach (var buildServer in buildServers)
            {
                services.AddSingleton(typeof(IBuildServer), buildServer);
            }
        }
    }
}
