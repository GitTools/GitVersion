using GitVersion.Git;
using GitVersion.Logging;
using LibGit2Sharp;

namespace GitVersion;

public static class LibGit2SharpExtensions
{
    extension(IRepository repository)
    {
        public IGitRepository ToGitRepository()
        {
            var gitRepository = new GitRepository(new NullLog());
            gitRepository.DiscoverRepository(repository.Info.Path);
            return gitRepository;
        }
    }
}
