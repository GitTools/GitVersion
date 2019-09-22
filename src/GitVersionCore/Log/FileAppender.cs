using System;

namespace GitVersion.Log
{
    public class FileAppender : ILogAppender
    {
        public void WriteTo(LogLevel level, string message)
        {
            throw new NotImplementedException();
        }
    }
}
