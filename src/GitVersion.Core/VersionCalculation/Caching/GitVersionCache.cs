using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public class GitVersionCache : IGitVersionCache
{
    private readonly IFileSystem fileSystem;
    private readonly ILog log;
    private readonly IGitRepositoryInfo repositoryInfo;

    public GitVersionCache(IFileSystem fileSystem, ILog log, IGitRepositoryInfo repositoryInfo)
    {
        this.fileSystem = fileSystem.NotNull();
        this.log = log.NotNull();
        this.repositoryInfo = repositoryInfo.NotNull();
    }

    public void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, GitVersionVariables variablesFromCache)
    {
        var cacheFileName = GetCacheFileName(cacheKey);
        using (this.log.IndentLog($"Write version variables to cache file {cacheFileName}"))
        {
            try
            {
                VersionVariablesHelper.ToFile(variablesFromCache, cacheFileName, this.fileSystem);
            }
            catch (Exception ex)
            {
                this.log.Error($"Unable to write cache file {cacheFileName}. Got {ex.GetType().FullName} exception.");
            }
        }
    }

    public GitVersionVariables? LoadVersionVariablesFromDiskCache(GitVersionCacheKey cacheKey)
    {
        var cacheFileName = GetCacheFileName(cacheKey);
        using (this.log.IndentLog($"Loading version variables from disk cache file {cacheFileName}"))
        {
            if (!this.fileSystem.Exists(cacheFileName))
            {
                this.log.Info("Cache file " + cacheFileName + " not found.");
                return null;
            }
            try
            {
                var loadedVariables = VersionVariablesHelper.FromFile(cacheFileName, this.fileSystem);
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

    internal string GetCacheFileName(GitVersionCacheKey cacheKey)
    {
        var cacheDir = PrepareCacheDirectory();
        return GetCacheFileName(cacheKey, cacheDir);
    }

    internal string GetCacheDirectory()
    {
        var gitDir = this.repositoryInfo.DotGitDirectory;
        return PathHelper.Combine(gitDir, "gitversion_cache");
    }

    private string PrepareCacheDirectory()
    {
        var cacheDir = GetCacheDirectory();

        // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
        this.fileSystem.CreateDirectory(cacheDir);

        return cacheDir;
    }

    private static string GetCacheFileName(GitVersionCacheKey key, string cacheDir) => PathHelper.Combine(cacheDir, string.Concat(key.Value, ".json"));
}
