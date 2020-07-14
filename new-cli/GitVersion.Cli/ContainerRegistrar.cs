using System;
using GitVersion.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Cli
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
        
        public IContainerRegistrar AddSingleton<TService>()
            where TService : class
            => AddSingleton<TService, TService>();
        
        public IContainerRegistrar AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            serviceCollection.AddSingleton(typeof(TService), implementationFactory);
            return this;
        }

        public IContainerRegistrar AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            serviceCollection.AddTransient<TService, TImplementation>();
            return this;
        }

        public IContainerRegistrar AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory) 
            where TService : class
        {
            serviceCollection.AddTransient(typeof(TService), implementationFactory);
            return this;
        }

        public IContainerRegistrar AddTransient<TService>()
            where TService : class
            => AddTransient<TService, TService>();

        public IContainer Build() => new Container(serviceCollection.BuildServiceProvider());
    }
}