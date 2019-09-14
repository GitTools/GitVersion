using System;
using GitVersion.Helpers;

namespace GitVersion.Log
{
    public sealed class NullLog : ILog
    {
        public void Write(LogLevel level, string format, params object[] args)
        {
        }

        public IDisposable IndentLog(string operationDescription)
        {
            return Disposable.Empty;
        }

        public string Indent { get; set; }
    }
}