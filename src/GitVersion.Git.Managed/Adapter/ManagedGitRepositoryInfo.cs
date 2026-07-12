using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using SysPath = System.IO.Path;

namespace GitVersion.Git;

internal sealed class ManagedGitRepositoryInfo : IGitRepositoryInfo
{
    private const string DotGitDirectoryNotFoundMessage = "Cannot find the .git directory";

    private readonly IFileSystem fileSystem;
    private readonly GitVersionOptions gitVersionOptions;

    private readonly Lazy<string?> dynamicGitRepositoryPath;
    private readonly Lazy<string?> dotGitDirectory;
    private readonly Lazy<string?> gitRootPath;
    private readonly Lazy<string?> projectRootDirectory;

    public ManagedGitRepositoryInfo(IFileSystem fileSystem, IOptions<GitVersionOptions> options)
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

    private string? GetDotGitDirectory()
    {
        var gitDirectory = !DynamicGitRepositoryPath.IsNullOrWhiteSpace()
            ? DynamicGitRepositoryPath
            : Discover(this.gitVersionOptions.WorkingDirectory)?.GitDirectory;

        gitDirectory = gitDirectory?.TrimEnd('/', '\\');
        if (string.IsNullOrEmpty(gitDirectory))
        {
            throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);
        }

        var directoryInfo = this.fileSystem.Directory.GetParent(gitDirectory) ?? throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);
        return gitDirectory.Contains(FileSystemHelper.Path.Combine(".git", "worktrees"))
            ? this.fileSystem.Directory.GetParent(directoryInfo.FullName)?.FullName
            : gitDirectory;
    }

    private string GetProjectRootDirectory()
    {
        if (!DynamicGitRepositoryPath.IsNullOrWhiteSpace())
        {
            return this.gitVersionOptions.WorkingDirectory;
        }

        var layout = Discover(this.gitVersionOptions.WorkingDirectory)
            ?? throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);

        var workingDirectory = layout.WorkingDirectory
            ?? throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);

        // Match libgit2's Info.WorkingDirectory, which carries a trailing directory separator.
        return workingDirectory.EndsWith(SysPath.DirectorySeparatorChar) || workingDirectory.EndsWith('/')
            ? workingDirectory
            : workingDirectory + SysPath.DirectorySeparatorChar;
    }

    private string? GetGitRootPath()
    {
        var isDynamicRepo = !DynamicGitRepositoryPath.IsNullOrWhiteSpace();
        var rootDirectory = isDynamicRepo ? DotGitDirectory : ProjectRootDirectory;

        return rootDirectory;
    }

    /// <summary>
    /// Discovers the repository containing <paramref name="startPath"/>, returning
    /// <see langword="null"/> when the path does not exist — matching libgit2's
    /// <c>Repository.Discover</c>, which never walks up from a nonexistent directory.
    /// </summary>
    private static GitRepositoryLayout? Discover(string startPath) =>
        Directory.Exists(startPath) ? GitRepositoryLayout.TryDiscover(startPath) : null;

    private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
    {
        try
        {
            var layout = GitRepositoryLayout.TryDiscover(possiblePath);
            if (layout is null)
            {
                return false;
            }

            var configuration = GitConfigurationFile.Load(SysPath.Combine(layout.CommonDirectory, "config"));
            return configuration.GetSubsections("remote")
                .Any(remoteName => configuration.GetString("remote", remoteName, "url") == targetUrl);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
