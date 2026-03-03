using System.IO.Abstractions;
using System.Security.Cryptography;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;

namespace GitVersion.VersionCalculation.Caching;

internal class GitVersionCacheKeyFactory(
    IFileSystem fileSystem,
    ILogger<GitVersionCacheKeyFactory> logger,
    IOptions<GitVersionOptions> options,
    IConfigurationFileLocator configFileLocator,
    IConfigurationSerializer configurationSerializer,
    IRepositoryStore repositoryStore,
    IGitRepositoryInfo repositoryInfo)
    : IGitVersionCacheKeyFactory
{
    private readonly ILogger<GitVersionCacheKeyFactory> logger = logger.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly IConfigurationFileLocator configFileLocator = configFileLocator.NotNull();
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public GitVersionCacheKey Create(IReadOnlyDictionary<object, object?>? overrideConfiguration)
    {
        var gitSystemHash = GetGitSystemHash();
        var configFileHash = GetConfigFileHash();
        var repositorySnapshotHash = GetRepositorySnapshotHash();
        var overrideConfigHash = GetOverrideConfigHash(overrideConfiguration);

        var compositeHash = GetHash(gitSystemHash, configFileHash, repositorySnapshotHash, overrideConfigHash);
        return new(compositeHash);
    }

    private string GetGitSystemHash()
    {
        var dotGitDirectory = this.repositoryInfo.DotGitDirectory;

        // traverse the directory and get a list of files, use that for GetHash
        var contents = CalculateDirectoryContents(FileSystemHelper.Path.Combine(dotGitDirectory, "refs"));

        return GetHash(contents);
    }

    // based on https://msdn.microsoft.com/en-us/library/bb513869.aspx
    private List<string> CalculateDirectoryContents(string root)
    {
        var result = new List<string>();

        // Data structure to hold names of subfolders to be
        // examined for files.
        var dirs = new Stack<string>();

        if (!this.fileSystem.Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Root directory does not exist: {root}");
        }

        dirs.Push(root);

        while (dirs.Count != 0)
        {
            var currentDir = dirs.Pop();

            var di = this.fileSystem.DirectoryInfo.New(currentDir);
            result.Add(di.Name);

            string[] subDirs;
            try
            {
                subDirs = this.fileSystem.Directory.GetDirectories(currentDir);
            }
            // An UnauthorizedAccessException exception will be thrown if we do not have
            // discovery permission on a folder or file. It may or may not be acceptable
            // to ignore the exception and continue enumerating the remaining files and
            // folders. It is also possible (but unlikely) that a DirectoryNotFound exception
            // will be raised. This will happen if currentDir has been deleted by
            // another application or thread after our call to Directory.Exists. The
            // choice of which exceptions to catch depends entirely on the specific task
            // you are intending to perform and also on how much you know with certainty
            // about the systems on which this code will run.
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
            {
                this.logger.LogError(ex, "{Message}", ex.Message);
                continue;
            }

            string[] files;
            try
            {
                files = this.fileSystem.Directory.GetFiles(currentDir);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
            {
                this.logger.LogError(ex, "{Message}", ex.Message);
                continue;
            }

            foreach (var file in files)
            {
                try
                {
                    if (!this.fileSystem.File.Exists(file)) continue;
                    result.Add(FileSystemHelper.Path.GetFileName(file));
                    result.Add(this.fileSystem.File.ReadAllText(file));
                }
                catch (IOException e)
                {
                    this.logger.LogError(e, "{Message}", e.Message);
                }
            }

            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            // push in reverse order
            for (var i = subDirs.Length - 1; i >= 0; i--)
            {
                dirs.Push(subDirs[i]);
            }
        }

        return result;
    }

    private string GetRepositorySnapshotHash()
    {
        var head = this.repositoryStore.Head;
        if (head.Tip == null)
        {
            return head.Name.Canonical;
        }

        var hash = string.Join(":", head.Name.Canonical, head.Tip.Sha);
        return GetHash(hash);
    }

    private string GetOverrideConfigHash(IReadOnlyDictionary<object, object?>? overrideConfiguration)
    {
        if (overrideConfiguration?.Any() != true)
        {
            return string.Empty;
        }

        // Doesn't depend on command line representation and
        // includes possible changes in default values of Config per se.
        var configContent = configurationSerializer.Serialize(overrideConfiguration);

        return GetHash(configContent);
    }

    private string GetConfigFileHash()
    {
        // will return the same hash even when configuration file will be moved
        // from workingDirectory to rootProjectDirectory. It's OK. Configuration essentially is the same.
        var workingDirectory = this.options.Value.WorkingDirectory;
        var projectRootDirectory = this.repositoryInfo.ProjectRootDirectory;

        var configFilePath = this.configFileLocator.GetConfigurationFile(workingDirectory)
                             ?? this.configFileLocator.GetConfigurationFile(projectRootDirectory);
        if (configFilePath == null || !this.fileSystem.File.Exists(configFilePath)) return string.Empty;

        var configFileContent = this.fileSystem.File.ReadAllText(configFilePath);
        return GetHash(configFileContent);
    }

    private static string GetHash(params IEnumerable<string> textsToHash)
    {
        var textToHash = string.Join(":", textsToHash);
        return GetHash(textToHash);
    }

    private static string GetHash(string textToHash)
    {
        if (textToHash.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(textToHash);
        var hashedBytes = SHA1.HashData(bytes);
        var hashedString = BitConverter.ToString(hashedBytes);
        return hashedString.Replace("-", "");
    }
}
