using System.Collections.Generic;
using LibGit2Sharp;
using ReferenceCollection = GitVersion.ReferenceCollection;

namespace GitVersionCore.Tests.Mocks
{
    public class MockReferenceCollection : ReferenceCollection
    {
        public override ReflogCollection Log(string canonicalName)
        {
            return new MockReflogCollection
            {
                Commits = Commits
            };
        }

        private List<Commit> Commits = new List<Commit>();

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
