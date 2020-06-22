using System;

namespace GitVersion.Core.Infrastructure
{
    public interface IContainer : IDisposable
    {
        T GetService<T>();
        object GetService(Type type);
    }
}