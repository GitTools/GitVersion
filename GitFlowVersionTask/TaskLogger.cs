namespace GitFlowVersionTask
{
    using Microsoft.Build.Framework;

    class TaskLogger
    {
        ITask task;

        public TaskLogger(ITask task)
        {
            this.task = task;
        }

        public void LogWarning(string message)
        {
            task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, null, 0, 0, 0, 0, message, string.Empty, "GitFlowVersionTask"));
        }

        public void LogInfo(string message)
        {
            task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitFlowVersionTask", MessageImportance.Normal));
        }

        public void LogError(string message, string file = null)
        {
            task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, file, 0, 0, 0, 0, message, string.Empty, "GitFlowVersionTask"));
        }
    }
}