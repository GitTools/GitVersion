using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddModule(this IServiceCollection serviceCollection, IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
            return serviceCollection;
        }
    }
}
