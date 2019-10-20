using System;

namespace GitVersion.Logging
{
    public sealed class NullLog : ILog
    {
        public Verbosity Verbosity { get; set; }

        public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
        {
        }

        public IDisposable IndentLog(string operationDescription)
        {
            return Disposable.Empty;
        }

        public void AddLogAppender(ILogAppender logAppender)
        {
        }

        public string Indent { get; set; }
    }
}
