using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockReferenceCollection : ReferenceCollection, ICollection<Commit>
    {

        public override ReflogCollection Log(string canonicalName)
        {
            return new MockReflogCollection
            {
                Commits = Commits
            };
        }

        public List<Commit> Commits = new List<Commit>();

        public new IEnumerator<Commit> GetEnumerator()
        {
            return Commits.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Commit item)
        {
            Commits.Add(item);
        }

        public void Clear()
        {
            Commits.Clear();
        }

        public bool Contains(Commit item)
        {
            return Commits.Contains(item);
        }

        public void CopyTo(Commit[] array, int arrayIndex)
        {
            Commits.CopyTo(array, arrayIndex);
        }

        public bool Remove(Commit item)
        {
            return Commits.Remove(item);
        }

        public int Count => Commits.Count;

        public bool IsReadOnly => false;
    }
}