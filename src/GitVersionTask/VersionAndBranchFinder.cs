﻿using System;
using System.Collections.Generic;
using GitVersion;
using GitVersion.Helpers;

public static class VersionAndBranchFinder
{
    static Dictionary<string, CachedVersion> versionCacheVersions = new Dictionary<string, CachedVersion>();

    public static bool TryGetVersion(string directory, out VersionVariables versionVariables, bool noFetch, Authentication authentication, IFileSystem fileSystem)
    {
        try
        {
            versionVariables = GetVersion(directory, authentication, noFetch, fileSystem);
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteWarning("Could not determine assembly version: " + ex.Message);
            versionVariables = null;
            return false;
        }
    }

    public static VersionVariables GetVersion(string directory, Authentication authentication, bool noFetch, IFileSystem fileSystem)
    {
        var gitDir = GitDirFinder.TreeWalkForDotGitDir(directory);
        using (var repo = RepositoryLoader.GetRepo(gitDir))
        {
            var ticks = DirectoryDateFinder.GetLastDirectoryWrite(gitDir);
            var key = string.Format("{0}:{1}:{2}",gitDir, repo.Head.CanonicalName, repo.Head.Tip.Sha);

            Logger.WriteInfo("CacheKey: " + key );
            CachedVersion result;
            if (versionCacheVersions.TryGetValue(key, out result))
            {
                if (result.Timestamp != ticks)
                {
                    Logger.WriteInfo(string.Format("Change detected. Flushing cache. OldTimeStamp: {0}. NewTimeStamp: {1}", result.Timestamp, ticks));
                    result.VersionVariables = ExecuteCore.ExecuteGitVersion(fileSystem, null, null, authentication, null, noFetch, directory, null);
                    result.Timestamp = ticks;
                }
                Logger.WriteInfo("Returning version from cache");
                return result.VersionVariables;
            }
            Logger.WriteInfo("Version not in cache. Calculating version.");

            return (versionCacheVersions[key] = new CachedVersion
            {
                VersionVariables = ExecuteCore.ExecuteGitVersion(fileSystem, null, null, authentication, null, noFetch, directory, null),
                Timestamp = ticks
            }).VersionVariables;
        }
    }
}
