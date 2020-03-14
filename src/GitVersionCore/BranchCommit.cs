using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// A commit, together with the branch to which the commit belongs.
    /// </summary>
    public readonly struct BranchCommit
    {
        public static readonly BranchCommit Empty = new BranchCommit();

        public BranchCommit(Commit commit, Branch branch) : this()
        {
            Branch = branch;
            Commit = commit;
        }

        public Branch Branch { get; }
        public Commit Commit { get; }

        private bool Equals(BranchCommit other)
        {
            return Equals(Branch, other.Branch) && Equals(Commit, other.Commit);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is BranchCommit commit && Equals(commit);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Branch != null ? Branch.GetHashCode() : 0) * 397) ^ (Commit != null ? Commit.GetHashCode() : 0);
            }
        }

        public static bool operator ==(BranchCommit left, BranchCommit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BranchCommit left, BranchCommit right)
        {
            return !left.Equals(right);
        }
    }
}
