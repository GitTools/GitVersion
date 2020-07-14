using System;

namespace GitVersion.Core.Infrastructure
{
    public interface IContainerRegistrar
    {
        IContainerRegistrar AddSingleton<TService>() where TService : class;

        IContainerRegistrar AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService;
        
        IContainerRegistrar AddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class;

        IContainerRegistrar AddTransient<TService>() where TService : class;

        IContainerRegistrar AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService;

        IContainerRegistrar AddTransient<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class;

        IContainer Build();
    }
}