using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public static class BuildLogger
{
    public static void LogDebug(this Task task, string message)
    {
        task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitVersionTask", MessageImportance.Low));
    }

    public static void LogInfo(this Task task, string message)
    {
        task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitVersionTask", MessageImportance.Normal));
    }

    public static void LogWarning(this Task task, string message)
    {
        task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, null, 0, 0, 0, 0, message, string.Empty, "GitVersionTask"));
    }

    public static void LogError(this Task task, string message, string file = null)
    {
        task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, file, 0, 0, 0, 0, message, string.Empty, "GitVersionTask"));
    }
}