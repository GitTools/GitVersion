using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.BuildAgents;

public class BuildServerModule : GitVersionModule
{
    public override void RegisterTypes(IServiceCollection services)
    {
        var buildAgents = FindAllDerivedTypes<BuildAgentBase>(Assembly.GetAssembly(GetType()));

        foreach (var buildAgent in buildAgents)
        {
            services.AddSingleton(typeof(IBuildAgent), buildAgent);
        }

        services.AddSingleton<IBuildAgentResolver, BuildAgentResolver>();
#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
        services.AddSingleton(sp => sp.GetService<IBuildAgentResolver>()?.Resolve());
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
    }
}
