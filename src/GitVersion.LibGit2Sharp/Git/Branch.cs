using System;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion
{
    internal sealed class Branch : IBranch
    {
        private static readonly LambdaEqualityHelper<IBranch> equalityHelper = new(x => x.CanonicalName);
        private static readonly LambdaKeyComparer<IBranch, string> comparerHelper = new(x => x.CanonicalName);

        private readonly LibGit2Sharp.Branch innerBranch;

        internal Branch(LibGit2Sharp.Branch branch) => innerBranch = branch;

        public int CompareTo(IBranch other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as IBranch)!);
        public bool Equals(IBranch other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public static implicit operator LibGit2Sharp.Branch(Branch d) => d.innerBranch;

        public string CanonicalName => innerBranch.CanonicalName;
        public string FriendlyName => innerBranch.FriendlyName;

        public string NameWithoutRemote =>
            IsRemote
                ? FriendlyName.Substring(FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1)
                : FriendlyName;

        public string NameWithoutOrigin =>
            IsRemote && FriendlyName.StartsWith("origin/")
                ? FriendlyName.Substring("origin/".Length)
                : FriendlyName;

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

        /// <summary>
        /// Checks if the two branch objects refer to the same branch (have the same friendly name).
        /// </summary>
        public bool IsSameBranch(IBranch otherBranch)
        {
            // For each branch, fixup the friendly name if the branch is remote.
            var otherBranchFriendlyName = otherBranch.NameWithoutRemote;
            return otherBranchFriendlyName.IsEquivalentTo(NameWithoutRemote);
        }
        public bool IsDetachedHead => CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);

        public bool IsRemote => innerBranch.IsRemote;
        public bool IsTracking => innerBranch.IsTracking;
    }
}
