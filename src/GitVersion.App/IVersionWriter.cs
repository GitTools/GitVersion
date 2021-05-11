using System;
using System.Reflection;

namespace GitVersion
{
    public interface IVersionWriter
    {
        void Write(Assembly assembly);
        void WriteTo(Assembly assembly, Action<string> writeAction);
    }
}
