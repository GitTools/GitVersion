namespace GitVersionTask
{
    using System.Collections.Concurrent;

    using GitVersion;
    using GitVersion.Helpers;

    using Microsoft.Build.Utilities;

    public abstract class GitVersionTaskBase : Task
    {
        static readonly ConcurrentDictionary<string, VersionVariables> versionVariablesCache;
        readonly ExecuteCore executeCore;


        static GitVersionTaskBase()
        {
            versionVariablesCache = new ConcurrentDictionary<string, VersionVariables>();
        }


        protected GitVersionTaskBase()
        {
            var fileSystem = new FileSystem();
            this.executeCore = new ExecuteCore(fileSystem, versionVariablesCache.GetOrAdd);
        }


        protected ExecuteCore ExecuteCore
        {
            get { return this.executeCore; }
        }
    }
}