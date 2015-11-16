namespace GitVersionTask
{
    using System.Collections.Concurrent;

    using GitVersion;
    using GitVersion.Helpers;

    using Microsoft.Build.Utilities;

    public abstract class GitVersionTaskBase : Task
    {
        static readonly ConcurrentDictionary<string, VersionVariables> versionVariablesCache;
        readonly VersionAndBranchFinder versionAndBranchFinder;


        static GitVersionTaskBase()
        {
            versionVariablesCache = new ConcurrentDictionary<string, VersionVariables>();
        }


        protected GitVersionTaskBase()
        {
            var fileSystem = new FileSystem();
            this.versionAndBranchFinder = new VersionAndBranchFinder(fileSystem, versionVariablesCache.GetOrAdd);
        }


        protected VersionAndBranchFinder VersionAndBranchFinder
        {
            get { return this.versionAndBranchFinder; }
        }
    }
}