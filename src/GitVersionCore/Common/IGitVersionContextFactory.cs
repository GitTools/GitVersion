using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitVersionContextFactory
    {
        GitVersionContext Init(IRepository repository, Branch currentBranch, string commitId = null, bool onlyTrackedBranches = false);
        GitVersionContext Create(Arguments arguments, IRepository repository);
    }
}
