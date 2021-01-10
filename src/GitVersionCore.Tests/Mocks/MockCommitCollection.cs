using System.Collections.Generic;
using System.Linq;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockCommitCollection : CommitCollection
    {
        private List<ICommit> Commits = new List<ICommit>();

        public override IEnumerator<ICommit> GetEnumerator()
        {
            return SortedBy == CommitSortStrategies.Reverse ? Commits.GetEnumerator() : Enumerable.Reverse(Commits).GetEnumerator();
        }

        public CommitSortStrategies SortedBy { get; set; }
        public void Add(ICommit item)
        {
            Commits.Add(item);
        }

        public void Clear()
        {
            Commits.Clear();
        }

        public bool Contains(ICommit item)
        {
            return Commits.Contains(item);
        }

        public void CopyTo(ICommit[] array, int arrayIndex)
        {
            Commits.CopyTo(array, arrayIndex);
        }

        public bool Remove(ICommit item)
        {
            return Commits.Remove(item);
        }

        public int Count => Commits.Count;

        public bool IsReadOnly => false;


        public override ICommitCollection QueryBy(CommitFilter commitFilter)
        {
            return this;
        }
    }
}
