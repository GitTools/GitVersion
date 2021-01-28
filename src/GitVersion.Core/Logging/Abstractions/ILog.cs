using System;

namespace GitVersion.Logging
{
    public interface ILog
    {
        Verbosity Verbosity { get; set; }
        void Write(Verbosity verbosity, LogLevel level, string format, params object[] args);
        IDisposable IndentLog(string operationDescription);
        void AddLogAppender(ILogAppender logAppender);
    }
}
