using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents;

public class GitVersionBuildAgentsModule : GitVersionModule
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
