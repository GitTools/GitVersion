using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public class GitVersionCache : IGitVersionCache
{
    private readonly IFileSystem fileSystem;
    private readonly IVersionVariableSerializer serializer;
    private readonly ILog log;
    private readonly IGitRepositoryInfo repositoryInfo;

    public GitVersionCache(IFileSystem fileSystem, IVersionVariableSerializer serializer, ILog log, IGitRepositoryInfo repositoryInfo)
    {
        this.fileSystem = fileSystem.NotNull();
        this.serializer = serializer.NotNull();
        this.log = log.NotNull();
        this.repositoryInfo = repositoryInfo.NotNull();
    }

    public void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, GitVersionVariables versionVariables)
    {
        var cacheFileName = GetCacheFileName(cacheKey);
        using (this.log.IndentLog($"Write version variables to cache file {cacheFileName}"))
        {
            try
            {
                serializer.ToFile(versionVariables, cacheFileName);
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
                this.log.Info($"Cache file {cacheFileName} not found.");
                return null;
            }
            try
            {
                var loadedVariables = serializer.FromFile(cacheFileName);
                return loadedVariables;
            }
            catch (Exception ex)
            {
                this.log.Warning($"Unable to read cache file {cacheFileName}, deleting it.");
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

    private static string GetCacheFileName(GitVersionCacheKey key, string cacheDir) => PathHelper.Combine(cacheDir, key.Value);
}
