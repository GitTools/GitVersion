using System.Collections.Generic;
using GitVersion;

public static class VersionCache
{
    static Dictionary<string, CachedVersion> versionCacheVersions = new Dictionary<string, CachedVersion>();

    public static CachedVersion GetVersion(string gitDirectory)
    {
        using (var repo = RepositoryLoader.GetRepo(gitDirectory))
        {
            var ticks = DirectoryDateFinder.GetLastDirectoryWrite(gitDirectory);
            var key = string.Format("{0}:{1}:{2}", repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);
            CachedVersion cachedVersion;
            if (versionCacheVersions.TryGetValue(key, out cachedVersion))
            {
                if (cachedVersion.Timestamp != ticks)
                {
                    Logger.WriteInfo("Change detected. flushing cache.");
                    cachedVersion.SemanticVersion = GitVersionFinder.GetSemanticVersion(repo);
                    cachedVersion.MasterReleaseDate = LastMinorVersionFinder.Execute(repo, repo.Head.Tip);
                }
                return cachedVersion;
            }
            Logger.WriteInfo("Version not in cache. Calculating version.");

            //TODO: cope with githubflow
            //if (GitVersionFinder.ShouldGitHubFlowVersioningSchemeApply(repo))
            //{
            //    return rd;
            //}
            return versionCacheVersions[key] = new CachedVersion
            {
                SemanticVersion = GitVersionFinder.GetSemanticVersion(repo),
                MasterReleaseDate = LastMinorVersionFinder.Execute(repo,repo.Head.Tip),
                Timestamp = ticks
            };

        }
    }

}