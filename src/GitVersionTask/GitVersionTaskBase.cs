namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;

    using Microsoft.Build.Utilities;

    public abstract class GitVersionTaskBase : Task
    {
        readonly ExecuteCore executeCore;

        protected GitVersionTaskBase()
        {
            var fileSystem = new FileSystem();
            executeCore = new ExecuteCore(fileSystem);
        }

        protected ExecuteCore ExecuteCore
        {
            get { return executeCore; }
        }
    }
}