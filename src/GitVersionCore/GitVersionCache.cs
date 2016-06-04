namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using GitVersion.Helpers;
    using LibGit2Sharp;
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

        private string PrepareCacheDirectory(GitPreparer gitPreparer)
        {
            var gitDir = gitPreparer.GetDotGitDirectory();
            
            // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
            var cacheDir = GetCacheDir(gitDir);
            fileSystem.CreateDirectory(cacheDir);

            return cacheDir;
        }

        public VersionVariables LoadVersionVariablesFromDiskCache(GitPreparer gitPreparer)
        {
            using (Logger.IndentLog("Loading version variables from disk cache"))
            {
                var cacheDir = PrepareCacheDirectory(gitPreparer);

                var cacheFileName = GetCacheFileName(GetKey(gitPreparer), cacheDir);
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

                return vv;
            }
        }

        string GetKey(GitPreparer gitPreparer)
        {
            var gitDir = gitPreparer.GetDotGitDirectory();

            // Maybe using timestamp in .git/refs directory is enough?
            var ticks = fileSystem.GetLastDirectoryWrite(Path.Combine(gitDir, "refs"));

            var configPath = ConfigurationProvider.SelectConfigFilePath(gitPreparer, fileSystem);
            var configText = fileSystem.Exists(configPath) ? fileSystem.ReadAllText(configPath) : null;
            var configHash = configText != null ? GetHash(configText) : null;

            return gitPreparer.WithRepository(repo => string.Join(":", gitDir, repo.Head.CanonicalName, repo.Head.Tip.Sha, ticks, configHash));
        }

        static string GetCacheFileName(string key, string cacheDir)
        {
            var cacheKey = GetHash(key);
            return string.Concat(Path.Combine(cacheDir, cacheKey), ".yml");
        }

        static string GetCacheDir(string gitDir)
        {
            return Path.Combine(gitDir, "gitversion_cache");
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