using GitVersion.Git;
using GitVersion.Logging;
using LibGit2Sharp;

namespace GitVersion;

public static class LibGit2SharpExtensions
{
    public static IGitRepository ToGitRepository(this IRepository repository)
    {
        var gitRepository = new GitRepository(new NullLog());
        gitRepository.DiscoverRepository(repository.Info.Path);
        return gitRepository;
    }
}
