using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    internal sealed class BranchCollection : IBranchCollection
    {
        private readonly LibGit2Sharp.BranchCollection innerCollection;
        internal BranchCollection(LibGit2Sharp.BranchCollection collection) => innerCollection = collection;

        public IEnumerator<IBranch> GetEnumerator()
        {
            return innerCollection.Select(branch => new Branch(branch)).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IBranch? this[string name]
        {
            get
            {
                var branch = innerCollection[name];
                return branch is null ? null : new Branch(branch);
            }
        }

        public IEnumerable<IBranch> ExcludeBranches(IEnumerable<IBranch> branchesToExclude) =>
            this.Where(b => branchesToExclude.All(bte => !b.Equals(bte)));
        public void UpdateTrackedBranch(IBranch branch, string remoteTrackingReferenceName) =>
            innerCollection.Update((Branch)branch, b => b.TrackedBranch = remoteTrackingReferenceName);
    }
}
