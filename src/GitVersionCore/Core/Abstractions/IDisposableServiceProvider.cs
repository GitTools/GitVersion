using System;

namespace GitVersion.Core.Abstractions
{
    public interface IDisposableServiceProvider : IServiceProvider, IDisposable
    { }
}
