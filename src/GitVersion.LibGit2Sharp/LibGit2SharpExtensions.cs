using GitVersion.Git;
using LibGit2Sharp;

namespace GitVersion;

public static class LibGit2SharpExtensions
{
    extension(IRepository repository)
    {
        public IGitRepository ToGitRepository()
        {
            var gitRepository = new GitRepository(NullLogger<GitRepository>.Instance);
            gitRepository.DiscoverRepository(repository.Info.Path);
            return gitRepository;
        }
    }
}
