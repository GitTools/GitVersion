using System;
using GitVersion.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GitVersion.Cli.Infrastructure
{
    public class ContainerRegistrar : IContainerRegistrar
    {
        private readonly ServiceCollection services = new ServiceCollection();

        public IContainerRegistrar AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            services.AddSingleton<TService, TImplementation>();
            return this;
        }
        
        public IContainerRegistrar AddSingleton<TService>()
            where TService : class
            => AddSingleton<TService, TService>();
        
        public IContainerRegistrar AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.AddSingleton(typeof(TService), implementationFactory);
            return this;
        }

        public IContainerRegistrar AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            services.AddTransient<TService, TImplementation>();
            return this;
        }

        public IContainerRegistrar AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory) 
            where TService : class
        {
            services.AddTransient(typeof(TService), implementationFactory);
            return this;
        }

        public IContainerRegistrar AddTransient<TService>()
            where TService : class
            => AddTransient<TService, TService>();
        
        public IContainerRegistrar AddConsoleLogging()
        {
            services.AddLogging(builder => builder.AddConsole());
            return this;
        }

        public IContainer Build() => new Container(services.BuildServiceProvider());
    }
}