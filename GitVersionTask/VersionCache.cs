using System;
using System.Collections.Generic;
using GitVersion;

public static class VersionCache
{
    static Dictionary<string, Tuple<CachedVersion, GitVersionContext>> versionCacheVersions = new Dictionary<string, Tuple<CachedVersion, GitVersionContext>>();

    public static Tuple<CachedVersion, GitVersionContext> GetVersion(string gitDirectory, Config configuration)
    {
        using (var repo = RepositoryLoader.GetRepo(gitDirectory))
        {
            var versionFinder = new GitVersionFinder();
            var context = new GitVersionContext(repo, configuration);
            var ticks = DirectoryDateFinder.GetLastDirectoryWrite(gitDirectory);
            var key = string.Format("{0}:{1}:{2}", repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);

            Tuple<CachedVersion, GitVersionContext> result;
            if (versionCacheVersions.TryGetValue(key, out result))
            {
                if (result.Item1.Timestamp != ticks)
                {
                    Logger.WriteInfo("Change detected. flushing cache.");
                    result.Item1.SemanticVersion = versionFinder.FindVersion(context);
                    result.Item1.MasterReleaseDate = LastMinorVersionFinder.Execute(repo, context.Configuration, repo.Head.Tip);
                }
                return result;
            }
            Logger.WriteInfo("Version not in cache. Calculating version.");

            return versionCacheVersions[key] = Tuple.Create(new CachedVersion
            {
                SemanticVersion = versionFinder.FindVersion(context),
                MasterReleaseDate = LastMinorVersionFinder.Execute(repo, context.Configuration, repo.Head.Tip),
                Timestamp = ticks
            }, context);
        }
    }

}