using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion.Helpers;
using GitVersion.OutputVariables;
using YamlDotNet.Serialization;
using GitVersion.Logging;

namespace GitVersion.Cache
{
    public class GitVersionCache : IGitVersionCache
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;

        public GitVersionCache(IFileSystem fileSystem, ILog log)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void WriteVariablesToDiskCache(IGitPreparer gitPreparer, GitVersionCacheKey cacheKey, VersionVariables variablesFromCache)
        {
            var cacheDir = PrepareCacheDirectory(gitPreparer);
            var cacheFileName = GetCacheFileName(cacheKey, cacheDir);

            variablesFromCache.FileName = cacheFileName;

            Dictionary<string, string> dictionary;
            using (log.IndentLog("Creating dictionary"))
            {
                dictionary = variablesFromCache.ToDictionary(x => x.Key, x => x.Value);
            }

            void WriteCacheOperation()
            {
                using var stream = fileSystem.OpenWrite(cacheFileName);
                using var sw = new StreamWriter(stream);
                using (log.IndentLog("Storing version variables to cache file " + cacheFileName))
                {
                    var serializer = new Serializer();
                    serializer.Serialize(sw, dictionary);
                }
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new ThreadSleep(), log, WriteCacheOperation, maxRetries: 6);
            retryOperation.ExecuteAsync().Wait();
        }

        public string GetCacheDirectory(IGitPreparer gitPreparer)
        {
            var gitDir = gitPreparer.GetDotGitDirectory();
            var cacheDir = Path.Combine(gitDir, "gitversion_cache");
            return cacheDir;
        }

        public VersionVariables LoadVersionVariablesFromDiskCache(IGitPreparer gitPreparer, GitVersionCacheKey key)
        {
            using (log.IndentLog("Loading version variables from disk cache"))
            {
                var cacheDir = PrepareCacheDirectory(gitPreparer);

                var cacheFileName = GetCacheFileName(key, cacheDir);
                if (!fileSystem.Exists(cacheFileName))
                {
                    log.Info("Cache file " + cacheFileName + " not found.");
                    return null;
                }

                using (log.IndentLog("Deserializing version variables from cache file " + cacheFileName))
                {
                    try
                    {
                        var loadedVariables = VersionVariables.FromFile(cacheFileName, fileSystem);
                        return loadedVariables;
                    }
                    catch (Exception ex)
                    {
                        log.Warning("Unable to read cache file " + cacheFileName + ", deleting it.");
                        log.Info(ex.ToString());
                        try
                        {
                            fileSystem.Delete(cacheFileName);
                        }
                        catch (Exception deleteEx)
                        {
                            log.Warning($"Unable to delete corrupted version cache file {cacheFileName}. Got {deleteEx.GetType().FullName} exception.");
                        }

                        return null;
                    }
                }
            }
        }

        private string PrepareCacheDirectory(IGitPreparer gitPreparer)
        {
            var cacheDir = GetCacheDirectory(gitPreparer);

            // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
            fileSystem.CreateDirectory(cacheDir);

            return cacheDir;
        }

        private static string GetCacheFileName(GitVersionCacheKey key, string cacheDir)
        {
            return Path.Combine(cacheDir, string.Concat(key.Value, ".yml"));
        }
    }
}
