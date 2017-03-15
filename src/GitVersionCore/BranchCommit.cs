namespace GitVersion
{
    using LibGit2Sharp;

    /// <summary>
    /// A commit, together with the branch to which the commit belongs.
    /// </summary>
    public struct BranchCommit
    {
        public static readonly BranchCommit Empty = new BranchCommit();

        public BranchCommit(Commit commit, Branch branch) : this()
        {
            Branch = branch;
            Commit = commit;
        }

        public Branch Branch { get; private set; }
        public Commit Commit { get; private set; }

        public bool Equals(BranchCommit other)
        {
            return Equals(Branch, other.Branch) && Equals(Commit, other.Commit);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is BranchCommit && Equals((BranchCommit)obj);
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