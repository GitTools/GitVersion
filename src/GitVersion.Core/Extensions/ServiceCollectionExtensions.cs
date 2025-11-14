using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddModule(IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
            return serviceCollection;
        }
    }

    extension(IServiceProvider serviceProvider)
    {
        public TService GetServiceForType<TService, TType>() =>
            serviceProvider.GetServices<TService>().Single(t => t?.GetType() == typeof(TType));
    }
}
