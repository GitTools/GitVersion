using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockCommitCollection : ICommitCollection
    {
        private List<ICommit> Commits = new List<ICommit>();

        public IEnumerator<ICommit> GetEnumerator()
        {
            return Enumerable.Reverse(Commits).GetEnumerator();
        }

        public void Add(ICommit item) => Commits.Add(item);

        public void Clear() => Commits.Clear();

        public bool Contains(ICommit item) => Commits.Contains(item);

        public void CopyTo(ICommit[] array, int arrayIndex) => Commits.CopyTo(array, arrayIndex);

        public bool Remove(ICommit item) => Commits.Remove(item);

        public int Count => Commits.Count;

        public IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan) => this;
        public ICommitCollection QueryBy(CommitFilter commitFilter) => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
