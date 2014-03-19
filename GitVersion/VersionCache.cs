namespace GitVersion
{
    using System.Collections.Generic;
    using LibGit2Sharp;

    public static class VersionCache
    {
        static Dictionary<string, CachedVersion> versionCacheVersions = new Dictionary<string, CachedVersion>();

        public static VersionAndBranchAndDate GetVersion(string gitDirectory)
        {
            using (var repo = RepositoryLoader.GetRepo(gitDirectory))
            {
                var branch = repo.Head;
                if (branch.Tip == null)
                {
                    throw new ErrorException("No Tip found. Has repo been initialized?");
                }
                var ticks = DirectoryDateFinder.GetLastDirectoryWrite(gitDirectory);
                var key = string.Format("{0}:{1}:{2}", repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);
                CachedVersion cachedVersion;
                VersionAndBranchAndDate versionAndBranch;
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
                        versionAndBranch = cachedVersion.VersionAndBranch = GetSemanticVersion(repo);
                    }
                }
                else
                {
                    Logger.WriteInfo("Version not in cache. Calculating version.");
                    versionAndBranch = GetSemanticVersion(repo);

                    versionCacheVersions[key] = new CachedVersion
                                                {
                                                    VersionAndBranch = versionAndBranch,
                                                    Timestamp = ticks
                                                };
                }

                return versionAndBranch;
            }
        }

        static VersionAndBranchAndDate GetSemanticVersion(Repository repository)
        {
            var versionForRepositoryFinder = new VersionForRepositoryFinder();
            return versionForRepositoryFinder.GetVersion(repository);
        }
    }
}