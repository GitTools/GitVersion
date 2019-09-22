using System;

namespace GitVersion.Log
{
    public interface ILog
    {
        Verbosity Verbosity { get; set; }
        void Write(Verbosity verbosity, LogLevel level, string format, params object[] args);
        IDisposable IndentLog(string operationDescription);
    }
}
