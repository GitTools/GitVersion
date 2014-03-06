namespace GitVersion
{
    using LibGit2Sharp;

    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        public IRepository Repository;
        public Branch CurrentBranch;
    }

}