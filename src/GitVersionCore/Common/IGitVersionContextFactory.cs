using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitVersionContextFactory
    {
        void Init(IRepository repository, Branch currentBranch, string commitId = null, bool onlyTrackedBranches = false);
        GitVersionContext Context { get; }
    }
}
