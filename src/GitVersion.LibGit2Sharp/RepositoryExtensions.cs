using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public static class RepositoryExtensions
    {
        public static IGitRepository ToGitRepository(this IRepository repository) => new GitRepository(repository);
        public static IGitRepositoryInfo ToGitRepositoryInfo(IOptions<GitVersionOptions> options) => new GitRepositoryInfo(options);
    }
}
