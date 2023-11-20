using GitVersion.Configuration.Init.Wizard;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration.Init;

public class GitVersionInitModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddTransient<IConfigInitWizard, ConfigInitWizard>();
        services.AddTransient<IConfigInitStepFactory, ConfigInitStepFactory>();

        var steps = IGitVersionModule.FindAllDerivedTypes<ConfigInitWizardStep>(Assembly.GetAssembly(GetType()));

        foreach (var step in steps)
        {
            services.AddTransient(step);
        }
    }
}
