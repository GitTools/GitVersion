using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    internal class MockBranchCollection : IBranchCollection
    {
        private List<IBranch> Branches = new List<IBranch>();

        public IEnumerator<IBranch> GetEnumerator()
        {
            return Branches.GetEnumerator();
        }

        public IBranch this[string friendlyName] => Branches.FirstOrDefault(x => x.FriendlyName == friendlyName);
        public IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude)
        {
            throw new System.NotImplementedException();
        }
        public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName)
        {
            throw new System.NotImplementedException();
        }

        public void Add(IBranch item)
        {
            Branches.Add(item);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
