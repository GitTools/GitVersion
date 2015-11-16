using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using GitVersion;
using GitVersion.Helpers;

using LibGit2Sharp;

using YamlDotNet.Serialization;

public class VersionAndBranchFinder
{
    readonly IFileSystem fileSystem;
    readonly Func<string, Func<string, VersionVariables>, VersionVariables> getVersionVariablesFromMemoryCache;


    public VersionAndBranchFinder(IFileSystem fileSystem, Func<string, Func<string, VersionVariables>, VersionVariables> getVersionVariablesFromMemoryCache = null)
    {
        if (fileSystem == null)
        {
            throw new ArgumentNullException("fileSystem");
        }

        this.getVersionVariablesFromMemoryCache = getVersionVariablesFromMemoryCache;
        this.fileSystem = fileSystem;
    }


    public bool TryGetVersion(string directory, out VersionVariables versionVariables, bool noFetch, Authentication authentication)
    {
        try
        {
            versionVariables = GetVersion(directory, authentication, noFetch);
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteWarning("Could not determine assembly version: " + ex);
            versionVariables = null;
            return false;
        }
    }


    public VersionVariables GetVersion(string directory, Authentication authentication, bool noFetch)
    {
        var gitDir = Repository.Discover(directory);
        using (var repo = this.fileSystem.GetRepository(gitDir))
        {
            // Maybe using timestamp in .git/refs directory is enough?
            var ticks = this.fileSystem.GetLastDirectoryWrite(Path.Combine(gitDir, "refs"));
            var key = string.Format("{0}:{1}:{2}:{3}", gitDir, repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks);

            if (this.getVersionVariablesFromMemoryCache != null)
            {
                return this.getVersionVariablesFromMemoryCache(key, k =>
                {
                    Logger.WriteInfo("Version not in memory cache. Attempting to load version from disk cache.");
                    return LoadVersionVariablesFromDiskCache(key, directory, authentication, noFetch, gitDir);
                });
            }

            return LoadVersionVariablesFromDiskCache(key, directory, authentication, noFetch, gitDir);
        }
    }


    VersionVariables LoadVersionVariablesFromDiskCache(string key, string directory, Authentication authentication, bool noFetch, string gitDir)
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
            this.fileSystem.CreateDirectory(cacheDir);

            var cacheFileName = string.Concat(Path.Combine(cacheDir, cacheKey), ".yml");
            VersionVariables vv = null;
            if (this.fileSystem.Exists(cacheFileName))
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
                            this.fileSystem.Delete(cacheFileName);
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
                vv = ExecuteCore.ExecuteGitVersion(this.fileSystem, null, null, authentication, null, noFetch, directory, null);
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