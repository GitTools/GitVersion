namespace GitVersion
{
    public class Branch : IBranch
    {
        private readonly LibGit2Sharp.Branch innerBranch;

        internal Branch(LibGit2Sharp.Branch branch)
        {
            innerBranch = branch;
        }

        protected Branch()
        {
        }
        public static implicit operator LibGit2Sharp.Branch(Branch d) => d?.innerBranch;

        public virtual string CanonicalName => innerBranch?.CanonicalName;
        public virtual string FriendlyName => innerBranch?.FriendlyName;
        public virtual ICommit Tip
        {
            get
            {
                var commit = innerBranch?.Tip;
                return commit is null ? null : new Commit(commit);
            }
        }

        public virtual ICommitCollection Commits
        {
            get
            {

                var commits = innerBranch?.Commits;
                return commits is null ? null : new CommitCollection(commits);
            }
        }

        public virtual bool IsRemote => innerBranch != null && innerBranch.IsRemote;
        public virtual bool IsTracking => innerBranch != null && innerBranch.IsTracking;
    }
}
