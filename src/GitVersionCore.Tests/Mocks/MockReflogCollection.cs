using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockReflogCollection : ReflogCollection
    {
        public List<Commit> Commits = new List<Commit>();
        public new IEnumerator<Commit> GetEnumerator()
        {
            return Commits.GetEnumerator();
        }

        public void Add(Commit item)
        {
            Commits.Add(item);
        }
    }
}
