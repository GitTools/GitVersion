using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using BranchCollection = GitVersion.BranchCollection;

namespace GitVersionCore.Tests.Mocks
{
    public class MockBranchCollection : BranchCollection
    {
        public List<Branch> Branches = new List<Branch>();

        public override IEnumerator<Branch> GetEnumerator()
        {
            return Branches.GetEnumerator();
        }

        public override Branch this[string friendlyName]
        {
            get { return Branches.FirstOrDefault(x => x.FriendlyName == friendlyName); }
        }

        public void Add(Branch item)
        {
            Branches.Add(item);
        }
    }
}
