using System;

namespace GitVersion.Infrastructure;

public interface IContainer : IDisposable
{
    T? GetService<T>();
    object? GetService(Type type);
}