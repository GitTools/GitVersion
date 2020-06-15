using Microsoft.Extensions.DependencyInjection;

namespace Core
{
    public class ContainerRegistrar : IContainerRegistrar
    {
        private readonly ServiceCollection serviceCollection = new ServiceCollection();

        public IContainerRegistrar AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            serviceCollection.AddSingleton<TService, TImplementation>();
            return this;
        }

        public IContainerRegistrar AddTransient<TService>()
            where TService : class
            => AddTransient<TService, TService>();


        public IContainerRegistrar AddSingleton<TService>()
            where TService : class
            => AddSingleton<TService, TService>();

        public IContainerRegistrar AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            serviceCollection.AddSingleton<TService, TImplementation>();
            return this;
        }

        public IContainer Build() => new Container(serviceCollection.BuildServiceProvider());
    }
}