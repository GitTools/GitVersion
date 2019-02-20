namespace GitVersion
{
    using GitVersion.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class GitVersionCache
    {
        readonly IFileSystem fileSystem;

        public GitVersionCache(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void WriteVariablesToDiskCache(GitPreparer gitPreparer, GitVersionCacheKey cacheKey, VersionVariables variablesFromCache)
        {
            var cacheDir = PrepareCacheDirectory(gitPreparer);
            var cacheFileName = GetCacheFileName(cacheKey, cacheDir);

            variablesFromCache.FileName = cacheFileName;

            Dictionary<string, string> dictionary;
            using (Logger.IndentLog("Creating dictionary"))
            {
                dictionary = variablesFromCache.ToDictionary(x => x.Key, x => x.Value);
            }

            Action writeCacheOperation = () =>
            {
                using (var stream = fileSystem.OpenWrite(cacheFileName))
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        using (Logger.IndentLog("Storing version variables to cache file " + cacheFileName))
                        {
                            var serializer = new Serializer();
                            serializer.Serialize(sw, dictionary);
                        }
                    }
                }
            };

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new ThreadSleep(), writeCacheOperation, maxRetries: 6);
            retryOperation.Execute();
        }

        public static string GetCacheDirectory(GitPreparer gitPreparer)
        {
            var gitDir = gitPreparer.GetDotGitDirectory();
            var cacheDir = Path.Combine(gitDir, "gitversion_cache");
            return cacheDir;
        }

        private string PrepareCacheDirectory(GitPreparer gitPreparer)
        {
            var cacheDir = GetCacheDirectory(gitPreparer);

            // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
            fileSystem.CreateDirectory(cacheDir);

            return cacheDir;
        }

        public VersionVariables LoadVersionVariablesFromDiskCache(GitPreparer gitPreparer, GitVersionCacheKey key)
        {
            using (Logger.IndentLog("Loading version variables from disk cache"))
            {
                var cacheDir = PrepareCacheDirectory(gitPreparer);

                var cacheFileName = GetCacheFileName(key, cacheDir);
                if (!fileSystem.Exists(cacheFileName))
                {
                    Logger.WriteInfo("Cache file " + cacheFileName + " not found.");
                    return null;
                }

                using (Logger.IndentLog("Deserializing version variables from cache file " + cacheFileName))
                {
                    try
                    {
                        var loadedVariables = VersionVariables.FromFile(cacheFileName, fileSystem);
                        return loadedVariables;
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

                        return null;
                    }
                }
            }
        }

        static string GetCacheFileName(GitVersionCacheKey key, string cacheDir)
        {
            return Path.Combine(cacheDir, string.Concat(key.Value, ".yml"));
        }
    }
}
