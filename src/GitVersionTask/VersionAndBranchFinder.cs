using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using GitVersion;
using GitVersion.Helpers;

using LibGit2Sharp;

using YamlDotNet.Serialization;

public static class VersionAndBranchFinder
{
    internal static ConcurrentDictionary<string, VersionVariables> VersionCacheVersions = new ConcurrentDictionary<string, VersionVariables>();


    public static bool TryGetVersion(string directory, out VersionVariables versionVariables, bool noFetch, Authentication authentication, IFileSystem fileSystem)
    {
        try
        {
            versionVariables = GetVersion(directory, authentication, noFetch, fileSystem);
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteWarning("Could not determine assembly version: " + ex);
            versionVariables = null;
            return false;
        }
    }


    public static VersionVariables GetVersion(string directory, Authentication authentication, bool noFetch, IFileSystem fileSystem)
    {
        var gitDir = Repository.Discover(directory);
        using (var repo = fileSystem.GetRepository(gitDir))
        {
            // Maybe using timestamp in .git/refs directory is enough?
            var ticks = fileSystem.GetLastDirectoryWrite(Path.Combine(gitDir, "refs"));
            string key = string.Format("{0}:{1}:{2}:{3}", gitDir, repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);

            return VersionCacheVersions.GetOrAdd(key, k =>
            {
                Logger.WriteInfo("Version not in memory cache. Attempting to load version from disk cache.");
                return LoadVersionVariablesFromDiskCache(k, directory, authentication, noFetch, fileSystem, gitDir, ticks);
            });
        }
    }


    static VersionVariables LoadVersionVariablesFromDiskCache(string key, string directory, Authentication authentication, bool noFetch, IFileSystem fileSystem, string gitDir, long ticks)
    {
        using (Logger.IndentLog("Loading version variables from disk cache"))
        {
            string cacheKey;
            using (var sha1 = SHA1.Create())
            {
                // Make a shorter key by hashing, to avoid having to long cache filename.
                cacheKey = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "");
            }

            var cacheDir = Path.Combine(gitDir, "gitversion_cache");
            // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
            fileSystem.CreateDirectory(cacheDir);

            var cacheFileName = string.Concat(Path.Combine(cacheDir, cacheKey), ".yml");
            VersionVariables vv = null;
            if (fileSystem.Exists(cacheFileName))
            {
                using (Logger.IndentLog("Deserializing version variables from cache file " + cacheFileName))
                {
                    try
                    {
                        vv = VersionVariables.FromFile(cacheFileName, fileSystem);
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
                            Logger.WriteWarning(string.Format("Unable to delete corrupted version cache file {0}. Got {1} exception.", cacheFileName, deleteEx.GetType().FullName));
                        }
                    }
                }
            }
            else
            {
                Logger.WriteInfo("Cache file " + cacheFileName + " not found.");
            }

            if (vv == null)
            {
                vv = ExecuteCore.ExecuteGitVersion(fileSystem, null, null, authentication, null, noFetch, directory, null);
                vv.FileName = cacheFileName;

                using (var stream = fileSystem.OpenWrite(cacheFileName))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        Dictionary<string, string> dictionary;
                        using (Logger.IndentLog("Creating dictionary"))
                        {
                            dictionary = vv.ToDictionary(x => x.Key, x => x.Value);
                        }

                        using (Logger.IndentLog("Storing version variables to cache file " + cacheFileName))
                        {
                            var serializer = new Serializer();
                            serializer.Serialize(sw, dictionary);
                        }
                    }
                }
            }

            return vv;
        }
    }
}