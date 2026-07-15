using System.IO.Abstractions;
using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.Git;

public class GitRepositoryInfo : IGitRepositoryInfo
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
        var repositoryInfo = this.gitVersionOptions.RepositoryInfo;
        return DynamicRepositoryPath.Get(this.fileSystem, repositoryInfo.TargetUrl, repositoryInfo.ClonePath, GitRepoHasMatchingRemote);
    }

    private string? GetDotGitDirectory() =>
        RepositoryPathResolution.ResolveDotGitDirectory(
            this.fileSystem,
            DynamicGitRepositoryPath,
            this.gitVersionOptions.WorkingDirectory,
            Repository.Discover);

    private string GetProjectRootDirectory() =>
        RepositoryPathResolution.ResolveProjectRootDirectory(
            DynamicGitRepositoryPath,
            this.gitVersionOptions.WorkingDirectory,
            static workingDirectory =>
            {
                var gitDirectory = Repository.Discover(workingDirectory);
                if (gitDirectory.IsNullOrEmpty())
                {
                    return null;
                }

                using var repo = new Repository(gitDirectory);
                return repo.Info.WorkingDirectory;
            });

    private string? GetGitRootPath() =>
        RepositoryPathResolution.ResolveGitRootPath(DynamicGitRepositoryPath, DotGitDirectory, ProjectRootDirectory);

    private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
    {
        try
        {
            using var gitRepository = new Repository(possiblePath);
            return gitRepository.Network.Remotes.Any(r => r.Url == targetUrl);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
