using System.Collections.Generic;
using System.Linq;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    public class MockBranchCollection : BranchCollection
    {
        public List<IBranch> Branches = new List<IBranch>();

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
