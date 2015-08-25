using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using GitVersion;
using GitVersion.Helpers;

using YamlDotNet.Serialization;

public static class VersionAndBranchFinder
{
    static ConcurrentDictionary<string, VersionVariables> versionCacheVersions = new ConcurrentDictionary<string, VersionVariables>();


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
            // Maybe using timestamp in .git/refs directory is enough?
            var ticks = DirectoryDateFinder.GetLastDirectoryWrite(Path.Combine(gitDir, "refs"));
            string key = string.Format("{0}:{1}:{2}:{3}", gitDir, repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);

            return versionCacheVersions.GetOrAdd(key, k =>
            {
                Logger.WriteInfo("Version not in memory cache. Attempting to load version from cache.");
                return LoadVersionVariablesFromDiskCache(k, directory, authentication, noFetch, fileSystem, gitDir, ticks);
            });
        }
    }


    static VersionVariables LoadVersionVariablesFromDiskCache(string key, string directory, Authentication authentication, bool noFetch, IFileSystem fileSystem, string gitDir, long ticks)
    {
        string cacheKey;
        using (var sha1 = SHA1.Create())
        {
            // Make a shorter key by hashing, to avoid having to long cache filename.
            cacheKey = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "");
        }

        var cacheDir = Path.Combine(gitDir, "gitversion_cache");
        if (!fileSystem.Exists(cacheDir))
        {
            Logger.WriteInfo("Creating directory for cache in " + cacheDir);
            fileSystem.CreateDirectory(cacheDir);
        }

        var cacheFileName = Path.Combine(cacheDir, cacheKey);
        VersionVariables vv = null;
        if (fileSystem.Exists(cacheFileName))
        {
            try
            {
                using (var stream = fileSystem.OpenRead(cacheFileName))
                using (var sr = new StreamReader(stream))
                {
                    Logger.WriteInfo("Deserializing version variables from cache file " + cacheFileName);
                    vv = VersionVariables.FromDictionary(new Deserializer().Deserialize<Dictionary<string, string>>(sr));
                    Logger.WriteInfo("Deserializing cache file done.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteWarning("Unable to read cache file " + cacheFileName + ", deleting it.");
                Logger.WriteInfo(ex.ToString());
                try
                {
                    fileSystem.Delete(cacheFileName);
                }
                catch (Exception deleteEx)
                {
                    Logger.WriteWarning(string.Format("Unable delete corrupted version cache file {0}. Got {1} exception.", cacheFileName, deleteEx.GetType().FullName));
                }
            }
        }
        if (vv == null)
        {
            vv = ExecuteCore.ExecuteGitVersion(fileSystem, null, null, authentication, null, noFetch, directory, null);

            using (var stream = fileSystem.OpenWrite(cacheFileName))
            using (var sw = new StreamWriter(stream))
            {
                Logger.WriteInfo("Storing version variables to cache file " + cacheFileName);
                var serializer = new Serializer();
                serializer.Serialize(sw, vv.ToDictionary(x => x.Key, x => x.Value));
                Logger.WriteInfo("Serialization of cache file done.");
            }
        }
        return vv;
    }
}