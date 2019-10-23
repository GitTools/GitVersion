using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddModule(this IServiceCollection serviceCollection, IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
        }
    }
}
