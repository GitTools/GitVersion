using System.Collections.Generic;
using GitVersion;

public static class VersionCache
{
    static Dictionary<string, CachedVersion> versionCacheVersions = new Dictionary<string, CachedVersion>();

    public static CachedVersion GetVersion(string gitDirectory, Config configuration)
    {
        using (var repo = RepositoryLoader.GetRepo(gitDirectory))
        {
            var versionFinder = new GitVersionFinder();
            var context = new GitVersionContext(repo, configuration);
            var ticks = DirectoryDateFinder.GetLastDirectoryWrite(gitDirectory);
            var key = string.Format("{0}:{1}:{2}", repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);
            CachedVersion cachedVersion;
            if (versionCacheVersions.TryGetValue(key, out cachedVersion))
            {
                if (cachedVersion.Timestamp != ticks)
                {
                    Logger.WriteInfo("Change detected. flushing cache.");
                    cachedVersion.SemanticVersion = versionFinder.FindVersion(context);
                    cachedVersion.MasterReleaseDate = LastMinorVersionFinder.Execute(repo, new Config(), repo.Head.Tip);
                }
                return cachedVersion;
            }
            Logger.WriteInfo("Version not in cache. Calculating version.");

            return versionCacheVersions[key] = new CachedVersion
            {
                SemanticVersion = versionFinder.FindVersion(context),
                MasterReleaseDate = LastMinorVersionFinder.Execute(repo, new Config(), repo.Head.Tip),
                Timestamp = ticks
            };

        }
    }

}