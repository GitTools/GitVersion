namespace GitVersion.Infrastructure;

public interface IContainer : IDisposable
{
    T? GetService<T>();
    T GetRequiredService<T>();
    object? GetService(Type type);
    object GetRequiredService(Type type);
}