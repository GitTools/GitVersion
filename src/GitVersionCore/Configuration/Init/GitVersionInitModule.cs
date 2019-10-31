using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GitVersion.Configuration.Init.Wizard;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration.Init
{
    public class GitVersionInitModule : IGitVersionModule
    {
        public void RegisterTypes(IServiceCollection services)
        {
            services.AddTransient<IConfigInitWizard, ConfigInitWizard>();
            services.AddTransient<IConfigInitStepFactory, ConfigInitStepFactory>();

            var steps = FindAllDerivedTypes<ConfigInitWizardStep>(Assembly.GetAssembly(GetType()));

            foreach (var step in steps)
            {
                services.AddTransient(step);
            }
        }

        private static IEnumerable<Type> FindAllDerivedTypes<T>(Assembly assembly)
        {
            var derivedType = typeof(T);
            return assembly.GetTypes().Where(t => t != derivedType && derivedType.IsAssignableFrom(t));
        } 
    }
}
