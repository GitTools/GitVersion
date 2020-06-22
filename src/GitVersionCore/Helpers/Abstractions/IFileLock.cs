using System;
using System.IO;

namespace GitVersion.Helpers.Abstractions
{
    public interface IFileLock : IDisposable
    {
        FileStream FileStream { get; }
    }
}
