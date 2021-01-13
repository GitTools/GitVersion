using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using GitVersion.Extensions;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockBranch : IBranch, ICollection<ICommit>
    {
        public MockBranch(string friendlyName)
        {
            this.friendlyName = friendlyName;
            CanonicalName = friendlyName;
        }

        private readonly MockCommitCollection commits = new MockCommitCollection();
        private readonly string friendlyName;
        public string FriendlyName => friendlyName;
        public string NameWithoutRemote =>
            IsRemote
                ? FriendlyName.Substring(FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1)
                : FriendlyName;

        public string NameWithoutOrigin =>
            IsRemote && FriendlyName.StartsWith("origin/")
                ? FriendlyName.Substring("origin/".Length)
                : FriendlyName;
        public bool IsSameBranch(IBranch otherBranch)
        {
            // For each branch, fixup the friendly name if the branch is remote.
            var otherBranchFriendlyName = otherBranch.NameWithoutRemote;
            return otherBranchFriendlyName.IsEquivalentTo(NameWithoutRemote);
        }
        public bool IsDetachedHead => CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);

        public ICommitCollection Commits => commits;
        public ICommit Tip => commits.First();
        public bool IsTracking => true;
        public bool IsRemote => false;
        public bool IsReadOnly => false;

        public string CanonicalName { get; }

        public override int GetHashCode()
        {
            return friendlyName.GetHashCode();
        }

        public bool Equals(IBranch other)
        {
            throw new NotImplementedException();
        }
        public int CompareTo(IBranch other)
        {
            throw new NotImplementedException();
        }
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public IEnumerator<ICommit> GetEnumerator()
        {
            return commits.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ICommit item)
        {
            commits.Add(item);
        }

        public void Clear()
        {
            commits.Clear();
        }

        public bool Contains(ICommit item)
        {
            return commits.Contains(item);
        }

        public void CopyTo(ICommit[] array, int arrayIndex)
        {
            commits.CopyTo(array, arrayIndex);
        }

        public bool Remove(ICommit item)
        {
            return commits.Remove(item);
        }

        public int Count => commits.Count;
    }
}
