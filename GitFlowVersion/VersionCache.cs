﻿namespace GitFlowVersion
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public static class VersionCache
    {
        static Dictionary<string, CachedVersion> versionCacheVersions = new Dictionary<string, CachedVersion>();

        public static VersionAndBranch GetVersion(string gitDirectory)
        {
            using (var repo = RepositoryLoader.GetRepo(gitDirectory))
            {
                var head = repo.FindBranch(repo.Head.Name);
                if (head.Tip == null)
                {
                    throw new ErrorException("No Tip found. Has repo been initialized?");
                }

                var ticks = DirectoryDateFinder.GetLastDirectoryWrite(gitDirectory);
                var key = string.Format("{0}:{1}:{2}", head.CanonicalName, head.Tip.Sha, ticks);
                CachedVersion cachedVersion;
                VersionAndBranch versionAndBranch;
                if (versionCacheVersions.TryGetValue(key, out cachedVersion))
                {
                    Logger.WriteInfo("Version read from cache.");
                    if (cachedVersion.Timestamp == ticks)
                    {
                        versionAndBranch = cachedVersion.VersionAndBranch;
                    }
                    else
                    {
                        Logger.WriteInfo("Change detected. flushing cache.");
                        versionAndBranch = cachedVersion.VersionAndBranch = GetSemanticVersion(repo, head);
                    }
                }
                else
                {
                    Logger.WriteInfo("Version not in cache. Calculating version.");
                    versionAndBranch = GetSemanticVersion(repo, head);

                    versionCacheVersions[key] = new CachedVersion
                                                {
                                                    VersionAndBranch = versionAndBranch,
                                                    Timestamp = ticks
                                                };
                }
                return versionAndBranch;
            }
        }

        static VersionAndBranch GetSemanticVersion(Repository repository, Branch branch)
        {
            var versionForRepositoryFinder = new VersionForRepositoryFinder();
            return versionForRepositoryFinder.GetVersion(repository, branch);
        }
    }
}