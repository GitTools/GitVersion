using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public static class BuildLogger 
{
    


    public static void LogWarning(this Task task, string message)
    {
        task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs("", "", null, 0, 0, 0, 0, message, "", "GitFlowVersionTask"));
    }

    public static void LogInfo(this Task task, string message)
    {
        task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, "", "GitFlowVersionTask", MessageImportance.Normal));
    }

    public static void LogError(this Task task, string message, string file = null)
    {
        task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs("", "", file, 0, 0, 0, 0, message, "", "GitFlowVersionTask"));
    }




}