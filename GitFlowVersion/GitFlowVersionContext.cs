namespace GitFlowVersion
{
    using LibGit2Sharp;

    public class GitFlowVersionContext
    {
        public Commit Tip;
        public IRepository Repository;
        public Branch CurrentBranch; 
    }
}