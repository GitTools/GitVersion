using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockBranch : Branch, ICollection<Commit>
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

        private readonly MockCommitLog commits = new MockCommitLog();
        private readonly string friendlyName;
        public override string FriendlyName => friendlyName;
        public override ICommitLog Commits => commits;
        public override Commit Tip => commits.First();
        public override bool IsTracking => true;

        public override string CanonicalName { get; }

        public override int GetHashCode()
        {
            return friendlyName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public IEnumerator<Commit> GetEnumerator()
        {
            return commits.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Commit item)
        {
            commits.Add(item);
        }

        public void Clear()
        {
            commits.Clear();
        }

        public bool Contains(Commit item)
        {
            return commits.Contains(item);
        }

        public void CopyTo(Commit[] array, int arrayIndex)
        {
            commits.CopyTo(array, arrayIndex);
        }

        public bool Remove(Commit item)
        {
            return commits.Remove(item);
        }

        public int Count => commits.Count;

        public bool IsReadOnly => false;
    }
}
