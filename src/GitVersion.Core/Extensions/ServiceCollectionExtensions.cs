namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="IServiceCollection"/> and <see cref="IServiceProvider"/> for GitVersion module registration.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>Registers all services declared by <paramref name="gitVersionModule"/> into <paramref name="serviceCollection"/>.</summary>
        public IServiceCollection AddModule(IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
            return serviceCollection;
        }
    }

    extension(IServiceProvider serviceProvider)
    {
        /// <summary>Returns the registered <typeparamref name="TService"/> whose concrete type is exactly <typeparamref name="TType"/>.</summary>
        public TService GetServiceForType<TService, TType>() =>
            serviceProvider.GetServices<TService>().Single(t => t?.GetType() == typeof(TType));
    }
}
