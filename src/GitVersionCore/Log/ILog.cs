using System;
using GitVersion.Helpers;

namespace GitVersion.Log
{
    public interface ILog
    {
        void Write(VerbosityLevel level, string format, params object[] args);
        IDisposable IndentLog(string operationDescription);
    }
}