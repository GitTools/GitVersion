using System.IO.Abstractions;
using GitVersion.Extensions;
using SysPath = System.IO.Path;

namespace GitVersion.Git;

internal sealed class ManagedGitRepositoryInfo : IGitRepositoryInfo
{
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

    private string? GetDotGitDirectory() =>
        RepositoryPathResolution.ResolveDotGitDirectory(
            this.fileSystem,
            DynamicGitRepositoryPath,
            this.gitVersionOptions.WorkingDirectory,
            static workingDirectory => Discover(workingDirectory)?.GitDirectory);

    private string GetProjectRootDirectory() =>
        RepositoryPathResolution.ResolveProjectRootDirectory(
            DynamicGitRepositoryPath,
            this.gitVersionOptions.WorkingDirectory,
            static workingDirectory =>
            {
                var repositoryWorkingDirectory = Discover(workingDirectory)?.WorkingDirectory;
                if (repositoryWorkingDirectory is null)
                {
                    return null;
                }

                // Match libgit2's Info.WorkingDirectory, which carries a trailing directory separator.
                return repositoryWorkingDirectory.EndsWith(SysPath.DirectorySeparatorChar) || repositoryWorkingDirectory.EndsWith('/')
                    ? repositoryWorkingDirectory
                    : repositoryWorkingDirectory + SysPath.DirectorySeparatorChar;
            });

    private string? GetGitRootPath() =>
        RepositoryPathResolution.ResolveGitRootPath(DynamicGitRepositoryPath, DotGitDirectory, ProjectRootDirectory);

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
            // Only possiblePath itself may be the repository, matching the libgit2 backend's
            // `new Repository(possiblePath)`; discovery walking up the hierarchy would match
            // an enclosing repository and hand a nonexistent .git path to the caller.
            var layout = GitRepositoryLayout.TryOpen(possiblePath);
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
