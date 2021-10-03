using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitRepositoryInfo : IGitRepositoryInfo
{
    private readonly IOptions<GitVersionOptions> options;
    private GitVersionOptions gitVersionOptions => this.options.Value;

    private readonly Lazy<string?> dynamicGitRepositoryPath;
    private readonly Lazy<string?> dotGitDirectory;
    private readonly Lazy<string?> gitRootPath;
    private readonly Lazy<string?> projectRootDirectory;

    public GitRepositoryInfo(IOptions<GitVersionOptions> options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        this.dynamicGitRepositoryPath = new Lazy<string?>(GetDynamicGitRepositoryPath);
        this.dotGitDirectory = new Lazy<string?>(GetDotGitDirectory);
        this.gitRootPath = new Lazy<string?>(GetGitRootPath);
        this.projectRootDirectory = new Lazy<string?>(GetProjectRootDirectory);
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
        var dynamicRepositoryClonePath = repositoryInfo.DynamicRepositoryClonePath;

        var userTemp = dynamicRepositoryClonePath ?? Path.GetTempPath();
        var repositoryName = targetUrl.Split('/', '\\').Last().Replace(".git", string.Empty);
        var possiblePath = Path.Combine(userTemp, repositoryName);

        // Verify that the existing directory is ok for us to use
        if (Directory.Exists(possiblePath) && !GitRepoHasMatchingRemote(possiblePath, targetUrl))
        {
            var i = 1;
            var originalPath = possiblePath;
            bool possiblePathExists;
            do
            {
                possiblePath = string.Concat(originalPath, "_", i++.ToString());
                possiblePathExists = Directory.Exists(possiblePath);
            } while (possiblePathExists && !GitRepoHasMatchingRemote(possiblePath, targetUrl));
        }

        var repositoryPath = Path.Combine(possiblePath, ".git");
        return repositoryPath;
    }

    private string? GetDotGitDirectory()
    {
        var gitDirectory = !DynamicGitRepositoryPath.IsNullOrWhiteSpace()
            ? DynamicGitRepositoryPath
            : GitRepository.Discover(gitVersionOptions.WorkingDirectory);

        gitDirectory = gitDirectory?.TrimEnd('/', '\\');
        if (gitDirectory.IsNullOrEmpty())
            throw new DirectoryNotFoundException("Cannot find the .git directory");

        return gitDirectory?.Contains(Path.Combine(".git", "worktrees")) == true
            ? Directory.GetParent(Directory.GetParent(gitDirectory).FullName).FullName
            : gitDirectory;
    }

    private string? GetProjectRootDirectory()
    {
        if (!DynamicGitRepositoryPath.IsNullOrWhiteSpace())
        {
            return gitVersionOptions.WorkingDirectory;
        }

        var gitDirectory = GitRepository.Discover(gitVersionOptions.WorkingDirectory);

        if (gitDirectory.IsNullOrEmpty())
            throw new DirectoryNotFoundException("Cannot find the .git directory");

        return new GitRepository(gitDirectory).WorkingDirectory;
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
            var gitRepository = new GitRepository(possiblePath);
            return gitRepository.Remotes.Any(r => r.Url == targetUrl);
        }
        catch (Exception)
        {
            return false;
        }
    }

}
