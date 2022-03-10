namespace GitVersion.Infrastructure;

public interface IContainer : IDisposable
{
    T? GetService<T>();
    T GetRequiredService<T>() where T : notnull;
    object? GetService(Type type);
    object GetRequiredService(Type type);
}
