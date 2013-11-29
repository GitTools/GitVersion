namespace GitFlowVersionTask
{
    using Microsoft.Build.Framework;

    internal class TaskLogger : GitFlowVersion.Integration.ILogger
    {
        private readonly ITask _task;

        public TaskLogger(ITask task)
        {
            _task = task;
        }

        public void LogWarning(string message)
        {
            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, null, 0, 0, 0, 0, message, string.Empty, "GitFlowVersionTask"));
        }

        public void LogInfo(string message)
        {
            _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitFlowVersionTask", MessageImportance.Normal));
        }

        public void LogError(string message, string file = null)
        {
            _task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, file, 0, 0, 0, 0, message, string.Empty, "GitFlowVersionTask"));
        }
    }
}
