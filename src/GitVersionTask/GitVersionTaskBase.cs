namespace GitVersionTask
{
    using System;
    using GitVersion;
    using GitVersion.Helpers;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public abstract class GitVersionTaskBase : Task
    {
        protected GitVersionTaskBase()
        {
            var fileSystem = new FileSystem();
            ExecuteCore = new ExecuteCore(fileSystem);
            GitVersion.Logger.SetLoggers(LogDebug, LogInfo, LogWarning, s => LogError(s));
        }

        public override bool Execute()
        {
            try
            {
                InnerExecute();
                return true;
            }
            catch (WarningException errorException)
            {
                LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                LogError("Error occurred: " + exception);
                return false;
            }
        }

        protected abstract void InnerExecute();

        protected ExecuteCore ExecuteCore { get; }

        [Required]
        public string SolutionDirectory { get; set; }

        public bool NoFetch { get; set; }

        public void LogDebug(string message)
        {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitVersionTask", MessageImportance.Low));
        }

        public void LogWarning(string message)
        {
            BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, null, 0, 0, 0, 0, message, string.Empty, "GitVersionTask"));
        }

        public void LogInfo(string message)
        {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, "GitVersionTask", MessageImportance.Normal));
        }

        public void LogError(string message, string file = null)
        {
            BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, file, 0, 0, 0, 0, message, string.Empty, "GitVersionTask"));
        }

        protected bool GetVersionVariables(out VersionVariables versionVariables)
        {
            return !ExecuteCore.TryGetVersion(SolutionDirectory, out versionVariables, NoFetch, new Authentication());
        }
    }
}
