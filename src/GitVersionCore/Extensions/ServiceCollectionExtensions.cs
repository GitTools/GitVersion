namespace GitVersion.Extensions
{
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static void AddModule(this IServiceCollection serviceCollection, IModule module)
        {
            module.RegisterTypes(serviceCollection);
        }
    }
}
