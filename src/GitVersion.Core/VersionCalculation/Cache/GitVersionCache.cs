using GitVersion.Cache;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using YamlDotNet.Serialization;

namespace GitVersion.VersionCalculation.Cache;

public class GitVersionCache : IGitVersionCache
{
    private readonly IFileSystem fileSystem;
    private readonly ILog log;
    private readonly IGitRepositoryInfo repositoryInfo;

    public GitVersionCache(IFileSystem fileSystem, ILog log, IGitRepositoryInfo repositoryInfo)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.repositoryInfo = repositoryInfo ?? throw new ArgumentNullException(nameof(repositoryInfo));
    }

    public void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, VersionVariables variablesFromCache)
    {
        var cacheDir = PrepareCacheDirectory();
        var cacheFileName = GetCacheFileName(cacheKey, cacheDir);

        variablesFromCache.FileName = cacheFileName;

        Dictionary<string, string> dictionary;
        using (this.log.IndentLog("Creating dictionary"))
        {
            dictionary = variablesFromCache.ToDictionary(x => x.Key, x => x.Value);
        }

        void WriteCacheOperation()
        {
            using var stream = this.fileSystem.OpenWrite(cacheFileName);
            using var sw = new StreamWriter(stream);
            using (this.log.IndentLog("Storing version variables to cache file " + cacheFileName))
            {
                var serializer = new Serializer();
                serializer.Serialize(sw, dictionary);
            }
        }

        var retryOperation = new RetryAction<IOException>(maxRetries: 6);
        retryOperation.Execute(WriteCacheOperation);
    }

    public string GetCacheDirectory()
    {
        var gitDir = this.repositoryInfo.DotGitDirectory;
        return Path.Combine(gitDir, "gitversion_cache");
    }

    public VersionVariables? LoadVersionVariablesFromDiskCache(GitVersionCacheKey key)
    {
        using (this.log.IndentLog("Loading version variables from disk cache"))
        {
            var cacheDir = PrepareCacheDirectory();

            var cacheFileName = GetCacheFileName(key, cacheDir);
            if (!this.fileSystem.Exists(cacheFileName))
            {
                this.log.Info("Cache file " + cacheFileName + " not found.");
                return null;
            }

            using (this.log.IndentLog("Deserializing version variables from cache file " + cacheFileName))
            {
                try
                {
                    var loadedVariables = VersionVariables.FromFile(cacheFileName, this.fileSystem, this.log);
                    return loadedVariables;
                }
                catch (Exception ex)
                {
                    this.log.Warning("Unable to read cache file " + cacheFileName + ", deleting it.");
                    this.log.Info(ex.ToString());
                    try
                    {
                        this.fileSystem.Delete(cacheFileName);
                    }
                    catch (Exception deleteEx)
                    {
                        this.log.Warning($"Unable to delete corrupted version cache file {cacheFileName}. Got {deleteEx.GetType().FullName} exception.");
                    }

                    return null;
                }
            }
        }
    }

    private string PrepareCacheDirectory()
    {
        var cacheDir = GetCacheDirectory();

        // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
        this.fileSystem.CreateDirectory(cacheDir);

        return cacheDir;
    }

    private static string GetCacheFileName(GitVersionCacheKey key, string cacheDir) => Path.Combine(cacheDir, string.Concat(key.Value, ".yml"));
}
