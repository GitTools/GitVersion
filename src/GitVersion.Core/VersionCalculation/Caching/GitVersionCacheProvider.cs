using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

internal class GitVersionCacheProvider(
    IFileSystem fileSystem,
    ILogger<GitVersionCacheProvider> logger,
    IOptions<GitVersionOptions> options,
    IVersionVariableSerializer serializer,
    IGitVersionCacheKeyFactory cacheKeyFactory,
    IGitRepositoryInfo repositoryInfo)
    : IGitVersionCacheProvider
{
    private readonly ILogger<GitVersionCacheProvider> logger = logger.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly IVersionVariableSerializer serializer = serializer.NotNull();
    private readonly IGitVersionCacheKeyFactory cacheKeyFactory = cacheKeyFactory.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public void WriteVariablesToDiskCache(GitVersionVariables versionVariables)
    {
        var cacheKey = GetCacheKey();
        var cacheFileName = GetCacheFileName(cacheKey);
        using (this.logger.StartIndentedScope($"Write version variables to cache file {cacheFileName}"))
        {
            try
            {
                serializer.ToFile(versionVariables, cacheFileName);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to write cache file {CacheFileName}. Got {ExceptionType} exception.", cacheFileName, ex.GetType().FullName);
            }
        }
    }

    public GitVersionVariables? LoadVersionVariablesFromDiskCache()
    {
        var cacheKey = GetCacheKey();
        var cacheFileName = GetCacheFileName(cacheKey);
        using (this.logger.StartIndentedScope($"Loading version variables from disk cache file {cacheFileName}"))
        {
            if (!this.fileSystem.File.Exists(cacheFileName))
            {
                this.logger.LogInformation("Cache file {CacheFileName} not found.", cacheFileName);
                return null;
            }

            try
            {
                var loadedVariables = serializer.FromFile(cacheFileName);
                return loadedVariables;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to read cache file {CacheFileName}, deleting it.", cacheFileName);
                try
                {
                    this.fileSystem.File.Delete(cacheFileName);
                }
                catch (Exception deleteEx)
                {
                    this.logger.LogError(deleteEx, "Unable to delete corrupted version cache file {CacheFileName}. Got {ExceptionType} exception.", cacheFileName, deleteEx.GetType().FullName);
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
        return FileSystemHelper.Path.Combine(gitDir, "gitversion_cache");
    }

    private string PrepareCacheDirectory()
    {
        var cacheDir = GetCacheDirectory();

        // If the cacheDir already exists, CreateDirectory just won't do anything (it won't fail). @asbjornu
        this.fileSystem.Directory.CreateDirectory(cacheDir);

        return cacheDir;
    }

    private GitVersionCacheKey GetCacheKey()
    {
        var cacheKey = this.cacheKeyFactory.Create(options.Value.ConfigurationInfo.OverrideConfiguration);
        return cacheKey;
    }

    private static string GetCacheFileName(GitVersionCacheKey key, string cacheDir) => FileSystemHelper.Path.Combine(cacheDir, key.Value);
}
