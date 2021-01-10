using System.Collections.Generic;
using System.Linq;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockBranchCollection : BranchCollection
    {
        private List<IBranch> Branches = new List<IBranch>();

        public override IEnumerator<IBranch> GetEnumerator()
        {
            return Branches.GetEnumerator();
        }

        public override IBranch this[string friendlyName]
        {
            get { return Branches.FirstOrDefault(x => x.FriendlyName == friendlyName); }
        }

        public void Add(IBranch item)
        {
            Branches.Add(item);
        }
    }
}
