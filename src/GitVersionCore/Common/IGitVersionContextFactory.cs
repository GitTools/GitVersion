using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitVersionContextFactory
    {
        GitVersionContext Create(IRepository repository, Branch currentBranch, string commitId = null, bool onlyTrackedBranches = false);
    }
}
