namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public abstract class GitVersionTaskBase : Task
    {
        readonly ExecuteCore executeCore;

        protected GitVersionTaskBase()
        {
            var fileSystem = new FileSystem();
            executeCore = new ExecuteCore(fileSystem);
            GitVersion.Logger.SetLoggers(this.LogDebug, this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        protected ExecuteCore ExecuteCore
        {
            get { return executeCore; }
        }

        public void LogDebug(string message)
        {
            this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitVersionTask", MessageImportance.Low));
        }

        public void LogWarning(string message)
        {
            this.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, null, 0, 0, 0, 0, message, string.Empty, "GitVersionTask"));
        }

        public void LogInfo(string message)
        {
            this.BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitVersionTask", MessageImportance.Normal));
        }

        public void LogError(string message, string file = null)
        {
            this.BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, file, 0, 0, 0, 0, message, string.Empty, "GitVersionTask"));
        }
    }
}