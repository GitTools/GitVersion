using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Build.Utilities;

namespace GitVersion.MsBuild;

internal class MsBuildAppender(TaskLoggingHelper taskLog) : ILogAppender
{
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
                taskLog.LogError(contents);
                break;
            case LogLevel.Warn:
                taskLog.LogWarning(contents);
                break;
            case LogLevel.Info:
            case LogLevel.Verbose:
            case LogLevel.Debug:
                taskLog.LogMessage(contents);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
