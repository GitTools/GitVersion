using System.Collections.Generic;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Cli.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IContainerRegistrar RegisterModule(this IContainerRegistrar serviceCollection,
            IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
            return serviceCollection;
        }
        
        public static IContainerRegistrar RegisterModules(this IContainerRegistrar serviceCollection,
            IEnumerable<IGitVersionModule> gitVersionModules)
        {
            foreach (var gitVersionModule in gitVersionModules)
            {
                gitVersionModule.RegisterTypes(serviceCollection);
            }
            return serviceCollection;
        }
    }
}