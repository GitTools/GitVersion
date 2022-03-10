using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Infrastructure;

public class Container : IContainer
{
    private readonly ServiceProvider serviceProvider;

    public Container(ServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

    public T? GetService<T>() => serviceProvider.GetService<T>();
    public T GetRequiredService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();

    public object? GetService(Type type) => serviceProvider.GetService(type);
    public object GetRequiredService(Type type) => serviceProvider.GetRequiredService(type);

    public void Dispose() => serviceProvider.Dispose();
}