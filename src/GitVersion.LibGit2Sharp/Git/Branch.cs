using System;
using GitVersion.Helpers;

namespace GitVersion
{
    internal sealed class Branch : IBranch
    {
        private static readonly LambdaEqualityHelper<IBranch> equalityHelper = new(x => x.Name.Canonical);
        private static readonly LambdaKeyComparer<IBranch, string> comparerHelper = new(x => x.Name.Canonical);

        private readonly LibGit2Sharp.Branch innerBranch;

        internal Branch(LibGit2Sharp.Branch branch)
        {
            innerBranch = branch;
            Name = new ReferenceName(branch.CanonicalName);
        }
        public ReferenceName Name { get; }

        public int CompareTo(IBranch other) => comparerHelper.Compare(this, other);
        public bool Equals(IBranch other) => equalityHelper.Equals(this, other);
        public override bool Equals(object obj) => Equals((obj as IBranch)!);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public override string ToString() => Name.ToString();
        public static implicit operator LibGit2Sharp.Branch(Branch d) => d.innerBranch;

        public ICommit? Tip
        {
            get
            {
                var commit = innerBranch.Tip;
                return commit is null ? null : new Commit(commit);
            }
        }

        public ICommitCollection? Commits
        {
            get
            {
                var commits = innerBranch.Commits;
                return commits is null ? null : new CommitCollection(commits);
            }
        }

        public bool IsDetachedHead => Name.Canonical.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);

        public bool IsRemote => innerBranch.IsRemote;
        public bool IsTracking => innerBranch.IsTracking;
    }
}
