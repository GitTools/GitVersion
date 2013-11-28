namespace GitFlowVersionTask
{
    using Microsoft.Build.Framework;

    internal class TaskLogger : GitFlowVersion.Integration.Interfaces.ILogger
    {
        private readonly ITask _task;

        public TaskLogger(ITask task)
        {
            _task = task;
        }

        public void LogWarning(string message)
        {
            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs("", "", null, 0, 0, 0, 0, message, "", "GitFlowVersionTask"));
        }

        public void LogInfo(string message)
        {
            _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, "", "GitFlowVersionTask", MessageImportance.Normal));
        }

        public void LogError(string message, string file = null)
        {
            _task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs("", "", file, 0, 0, 0, 0, message, "", "GitFlowVersionTask"));
        }
    }
}
