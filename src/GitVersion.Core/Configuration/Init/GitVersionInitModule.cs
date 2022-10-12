using GitVersion.Configurations.Init.Wizard;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configurations.Init;

public class GitVersionInitModule : GitVersionModule
{
    public override void RegisterTypes(IServiceCollection services)
    {
        services.AddTransient<IConfigInitWizard, ConfigInitWizard>();
        services.AddTransient<IConfigInitStepFactory, ConfigInitStepFactory>();

        var steps = FindAllDerivedTypes<ConfigInitWizardStep>(Assembly.GetAssembly(GetType()));

        foreach (var step in steps)
        {
            services.AddTransient(step);
        }
    }
}
