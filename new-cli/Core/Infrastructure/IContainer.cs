using System;

namespace Core
{
    public interface IContainer : IDisposable
    {
        T GetService<T>();
        object GetService(Type type);
    }
}