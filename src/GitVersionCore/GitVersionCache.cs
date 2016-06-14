namespace GitVersion
{
    using GitVersion.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using YamlDotNet.Serialization;

    public class GitVersionCache
    {
        readonly IFileSystem fileSystem;

        public GitVersionCache(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void WriteVariablesToDiskCache(GitPreparer gitPreparer, VersionVariables variablesFromCache)
        {
            var cacheDir = PrepareCacheDirectory(gitPreparer);
            var cacheFileName = GetCacheFileName(GetKey(gitPreparer), cacheDir);
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

        public VersionVariables LoadVersionVariablesFromDiskCache(GitPreparer gitPreparer)
        {
            using (Logger.IndentLog("Loading version variables from disk cache"))
            {
                var cacheDir = PrepareCacheDirectory(gitPreparer);

                var cacheFileName = GetCacheFileName(GetKey(gitPreparer), cacheDir);
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

        string GetKey(GitPreparer gitPreparer)
        {
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();

            // Maybe using timestamp in .git/refs directory is enough?
            var lastGitRefsChangedTicks = fileSystem.GetLastDirectoryWrite(Path.Combine(dotGitDirectory, "refs"));

            // will return the same hash even when config file will be moved 
            // from workingDirectory to rootProjectDirectory. It's OK. Config essentially is the same.
            var configFilePath = ConfigurationProvider.SelectConfigFilePath(gitPreparer, fileSystem);
            var configFileContent = fileSystem.Exists(configFilePath) ? fileSystem.ReadAllText(configFilePath) : null;
            var configFileHash = configFileContent != null ? GetHash(configFileContent) : null;

            return gitPreparer.WithRepository(repo => string.Join(":", dotGitDirectory, repo.Head.CanonicalName, repo.Head.Tip.Sha, lastGitRefsChangedTicks, configFileHash));
        }

        static string GetCacheFileName(string key, string cacheDir)
        {
            var cacheKey = GetHash(key);
            return string.Concat(Path.Combine(cacheDir, cacheKey), ".yml");
        }

        static string GetHash(string textToHash)
        {
            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(textToHash);
                var hashedBytes = sha1.ComputeHash(bytes);
                var hashedString = BitConverter.ToString(hashedBytes);
                return hashedString.Replace("-", "");
            }
        }
    }
}
