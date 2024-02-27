using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Infrastructure;

public sealed class Container(ServiceProvider serviceProvider) : IContainer
{
    private readonly ServiceProvider serviceProvider = serviceProvider.NotNull();

    public T? GetService<T>() => serviceProvider.GetService<T>();
    public T GetRequiredService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();

    public object GetService(Type type) => serviceProvider.GetRequiredService(type);
    public object GetRequiredService(Type type) => serviceProvider.GetRequiredService(type);

    public void Dispose()
    {
        Dispose(true);
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);  // Violates rule
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            serviceProvider.Dispose();
        }
    }
}
