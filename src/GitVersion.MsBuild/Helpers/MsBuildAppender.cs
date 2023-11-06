using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Build.Utilities;

namespace GitVersion.MsBuild;

internal class MsBuildAppender : ILogAppender
{
    private readonly TaskLoggingHelper taskLog;

    public MsBuildAppender(TaskLoggingHelper taskLog) => this.taskLog = taskLog;

    public void WriteTo(LogLevel level, string message)
    {
        try
        {
            WriteLogEntry(level, message);
        }
        catch
        {
            //
        }
    }

    private void WriteLogEntry(LogLevel level, string str)
    {
        var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{str}{PathHelper.NewLine}";
        switch (level)
        {
            case LogLevel.Fatal:
            case LogLevel.Error:
                this.taskLog.LogError(contents);
                break;
            case LogLevel.Warn:
                this.taskLog.LogWarning(contents);
                break;
            case LogLevel.Info:
            case LogLevel.Verbose:
            case LogLevel.Debug:
                this.taskLog.LogMessage(contents);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
