namespace GitVersion
{
    using System.Collections.Generic;
    using LibGit2Sharp;

    public static class VersionCache
    {
        static Dictionary<string, CachedVersion> versionCacheVersions = new Dictionary<string, CachedVersion>();

        public static SemanticVersion GetVersion(string gitDirectory)
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
                SemanticVersion versionAndBranch;
                if (versionCacheVersions.TryGetValue(key, out cachedVersion))
                {
                    Logger.WriteInfo("Version read from cache.");
                    if (cachedVersion.Timestamp == ticks)
                    {
                        versionAndBranch = cachedVersion.SemanticVersion;
                    }
                    else
                    {
                        Logger.WriteInfo("Change detected. flushing cache.");
                        versionAndBranch = cachedVersion.SemanticVersion = GetSemanticVersion(repo);
                    }
                }
                else
                {
                    Logger.WriteInfo("Version not in cache. Calculating version.");
                    versionAndBranch = GetSemanticVersion(repo);

                    versionCacheVersions[key] = new CachedVersion
                                                {
                                                    SemanticVersion = versionAndBranch,
                                                    Timestamp = ticks
                                                };
                }

                return versionAndBranch;
            }
        }

        static SemanticVersion GetSemanticVersion(Repository repository)
        {
            var versionForRepositoryFinder = new GitVersionFinder();
            var gitVersionContext = new GitVersionContext(repository);
            Logger.WriteInfo("Running against branch: " + gitVersionContext.CurrentBranch.Name);
            return versionForRepositoryFinder.FindVersion(gitVersionContext);
        }
    }
}