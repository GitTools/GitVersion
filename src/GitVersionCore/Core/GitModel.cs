using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersion
{
    public class BranchCollection : IEnumerable<Branch>
    {
        private readonly LibGit2Sharp.BranchCollection innerBranchCollection;
        private BranchCollection(LibGit2Sharp.BranchCollection branchCollection) => innerBranchCollection = branchCollection;

        protected BranchCollection()
        {
        }

        public static implicit operator LibGit2Sharp.BranchCollection(BranchCollection d) => d.innerBranchCollection;
        public static explicit operator BranchCollection(LibGit2Sharp.BranchCollection b) => new BranchCollection(b);

        public virtual IEnumerator<Branch> GetEnumerator()
        {
            foreach (var branch in innerBranchCollection)
                yield return branch;
        }

        public virtual Branch Add(string name, Commit commit)
        {
            return innerBranchCollection.Add(name, commit);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Branch this[string friendlyName] => innerBranchCollection[friendlyName];
        public void Update(Branch branch, params Action<BranchUpdater>[] actions)
        {
            innerBranchCollection.Update(branch, actions);
        }
    }
}
