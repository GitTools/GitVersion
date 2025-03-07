using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion.Git;

internal class GitRepositoryInfo : IGitRepositoryInfo
{
    private readonly IFileSystem fileSystem;
    private readonly GitVersionOptions gitVersionOptions;

    private readonly Lazy<string?> dynamicGitRepositoryPath;
    private readonly Lazy<string?> dotGitDirectory;
    private readonly Lazy<string?> gitRootPath;
    private readonly Lazy<string?> projectRootDirectory;

    public GitRepositoryInfo(IFileSystem fileSystem, IOptions<GitVersionOptions> options)
    {
        this.fileSystem = fileSystem.NotNull();
        this.gitVersionOptions = options.NotNull().Value;

        this.dynamicGitRepositoryPath = new(GetDynamicGitRepositoryPath);
        this.dotGitDirectory = new(GetDotGitDirectory);
        this.gitRootPath = new(GetGitRootPath);
        this.projectRootDirectory = new(GetProjectRootDirectory);
    }

    public string? DynamicGitRepositoryPath => this.dynamicGitRepositoryPath.Value;
    public string? DotGitDirectory => this.dotGitDirectory.Value;
    public string? GitRootPath => this.gitRootPath.Value;
    public string? ProjectRootDirectory => this.projectRootDirectory.Value;

    private string? GetDynamicGitRepositoryPath()
    {
        var repositoryInfo = gitVersionOptions.RepositoryInfo;
        if (repositoryInfo.TargetUrl.IsNullOrWhiteSpace()) return null;

        var targetUrl = repositoryInfo.TargetUrl;
        var clonePath = repositoryInfo.ClonePath;

        var userTemp = clonePath ?? PathHelper.GetTempPath();
        var repositoryName = targetUrl.Split('/', '\\').Last().Replace(".git", string.Empty);
        var possiblePath = PathHelper.Combine(userTemp, repositoryName);

        // Verify that the existing directory is ok for us to use
        if (this.fileSystem.Directory.Exists(possiblePath) && !GitRepoHasMatchingRemote(possiblePath, targetUrl))
        {
            var i = 1;
            var originalPath = possiblePath;
            bool possiblePathExists;
            do
            {
                possiblePath = $"{originalPath}_{i++}";
                possiblePathExists = this.fileSystem.Directory.Exists(possiblePath);
            } while (possiblePathExists && !GitRepoHasMatchingRemote(possiblePath, targetUrl));
        }

        var repositoryPath = PathHelper.Combine(possiblePath, ".git");
        return repositoryPath;
    }

    private string? GetDotGitDirectory()
    {
        var gitDirectory = !DynamicGitRepositoryPath.IsNullOrWhiteSpace()
            ? DynamicGitRepositoryPath
            : Repository.Discover(gitVersionOptions.WorkingDirectory);

        gitDirectory = gitDirectory?.TrimEnd('/', '\\');
        if (gitDirectory.IsNullOrEmpty())
            throw new DirectoryNotFoundException("Cannot find the .git directory");

        var directoryInfo = this.fileSystem.Directory.GetParent(gitDirectory) ?? throw new DirectoryNotFoundException("Cannot find the .git directory");
        return gitDirectory.Contains(PathHelper.Combine(".git", "worktrees"))
            ? this.fileSystem.Directory.GetParent(directoryInfo.FullName)?.FullName
            : gitDirectory;
    }

    private string GetProjectRootDirectory()
    {
        if (!DynamicGitRepositoryPath.IsNullOrWhiteSpace())
        {
            return gitVersionOptions.WorkingDirectory;
        }

        var gitDirectory = Repository.Discover(gitVersionOptions.WorkingDirectory);

        if (gitDirectory.IsNullOrEmpty())
            throw new DirectoryNotFoundException("Cannot find the .git directory");

        return new Repository(gitDirectory).Info.WorkingDirectory;
    }

    private string? GetGitRootPath()
    {
        var isDynamicRepo = !DynamicGitRepositoryPath.IsNullOrWhiteSpace();
        var rootDirectory = isDynamicRepo ? DotGitDirectory : ProjectRootDirectory;

        return rootDirectory;
    }

    private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
    {
        try
        {
            var gitRepository = new Repository(possiblePath);
            return gitRepository.Network.Remotes.Any(r => r.Url == targetUrl);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
