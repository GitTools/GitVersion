namespace Core
{
    public static class ServiceCollectionExtensions
    {
        public static IContainerRegistrar RegisterModule(this IContainerRegistrar serviceCollection,
            IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
            return serviceCollection;
        }
    }
}