using System;
using Microsoft.Build.Utilities;

namespace GitVersion.Logging
{
    public class MsBuildAppender : ILogAppender
    {
        private readonly TaskLoggingHelper taskLog;

        public MsBuildAppender(TaskLoggingHelper taskLog)
        {
            this.taskLog = taskLog;
        }

        public void WriteTo(LogLevel level, string message)
        {
            try
            {
                if (level != LogLevel.None)
                {
                    WriteLogEntry(level, message);
                }
            }
            catch (Exception)
            {
                // 
            }
        }

        private void WriteLogEntry(LogLevel level, string str)
        {
            var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{str}\r\n";
            switch (level)
            {
                case LogLevel.None:
                    break;
                case LogLevel.Error:
                    taskLog.LogError(contents);
                    break;
                case LogLevel.Warn:
                    taskLog.LogWarning(contents);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                    taskLog.LogMessage(contents);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
