using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation.Caching;

internal class GitVersionCacheProvider(
    IFileSystem fileSystem,
    ILog log,
    IOptions<GitVersionOptions> options,
    IVersionVariableSerializer serializer,
    IGitVersionCacheKeyFactory cacheKeyFactory,
    IGitRepositoryInfo repositoryInfo)
    : IGitVersionCacheProvider
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly ILog log = log.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly IVersionVariableSerializer serializer = serializer.NotNull();
    private readonly IGitVersionCacheKeyFactory cacheKeyFactory = cacheKeyFactory.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public void WriteVariablesToDiskCache(GitVersionVariables versionVariables)
    {
        var cacheKey = GetCacheKey();
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

    public GitVersionVariables? LoadVersionVariablesFromDiskCache()
    {
        var cacheKey = GetCacheKey();
        var cacheFileName = GetCacheFileName(cacheKey);
        using (this.log.IndentLog($"Loading version variables from disk cache file {cacheFileName}"))
        {
            if (!this.fileSystem.File.Exists(cacheFileName))
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
                    this.fileSystem.File.Delete(cacheFileName);
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
        this.fileSystem.Directory.CreateDirectory(cacheDir);

        return cacheDir;
    }

    private GitVersionCacheKey GetCacheKey()
    {
        var cacheKey = this.cacheKeyFactory.Create(options.Value.ConfigurationInfo.OverrideConfiguration);
        return cacheKey;
    }

    private static string GetCacheFileName(GitVersionCacheKey key, string cacheDir) => PathHelper.Combine(cacheDir, key.Value);
}
