namespace GitVersion.Infrastructure;

public interface IContainerRegistrar
{
    IContainerRegistrar AddSingleton<TService>() where TService : class;

    IContainerRegistrar AddSingleton<TService, TImplementation>()
        where TService : class where TImplementation : class, TService;

    IContainerRegistrar AddTransient<TService>() where TService : class;

    IContainerRegistrar AddTransient<TService, TImplementation>()
        where TService : class where TImplementation : class, TService;

    IContainerRegistrar AddLogging(string[] args);

    IContainer Build();
}