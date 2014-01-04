namespace GitFlowVersion
{
    using LibGit2Sharp;

    /// <summary>
    /// Contextual information about where GitFlowVersion is being run
    /// </summary>
    public class GitFlowVersionContext
    {
        public IRepository Repository;
        public Branch CurrentBranch;
    }

}