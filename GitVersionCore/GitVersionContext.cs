namespace GitVersion
{
    using LibGit2Sharp;

    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        public GitVersionContext(IRepository repository)
            : this(repository, repository.Head)
        {
        }

        public GitVersionContext(IRepository repository, Branch currentBranch)
        {
            Repository = repository;
            CurrentBranch = currentBranch;
            CurrentCommit = CurrentBranch.Tip;
        }

        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }
    }
}