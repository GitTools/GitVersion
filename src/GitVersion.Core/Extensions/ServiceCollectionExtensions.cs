using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModule(this IServiceCollection serviceCollection, IGitVersionModule gitVersionModule)
    {
        gitVersionModule.RegisterTypes(serviceCollection);
        return serviceCollection;
    }

    public static TService GetServiceForType<TService, TType>(this IServiceProvider serviceProvider) =>
        serviceProvider.GetServices<TService>().SingleOrDefault(t => t?.GetType() == typeof(TType));
}
