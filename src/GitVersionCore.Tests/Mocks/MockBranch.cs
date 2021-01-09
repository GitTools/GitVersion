using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    public class MockBranch : Branch, ICollection<ICommit>
    {
        public MockBranch(string friendlyName)
        {
            this.friendlyName = friendlyName;
            CanonicalName = friendlyName;
        }
        public MockBranch(string friendlyName, string canonicalName)
        {
            this.friendlyName = friendlyName;
            CanonicalName = canonicalName;
        }

        public MockBranch()
        {

        }

        private readonly MockCommitCollection commits = new MockCommitCollection();
        private readonly string friendlyName;
        public override string FriendlyName => friendlyName;
        public override ICommitCollection Commits => commits;
        public override ICommit Tip => commits.First();
        public override bool IsTracking => true;
        public override bool IsRemote => false;

        public override string CanonicalName { get; }

        public override int GetHashCode()
        {
            return friendlyName.GetHashCode();
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

        public bool IsReadOnly => false;
    }
}
