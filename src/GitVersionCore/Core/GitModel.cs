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

    public class TagCollection : IEnumerable<Tag>
    {
        private readonly LibGit2Sharp.TagCollection innerTagCollection;
        private TagCollection(LibGit2Sharp.TagCollection branchCollection) => innerTagCollection = branchCollection;

        protected TagCollection()
        {
        }

        public static implicit operator LibGit2Sharp.TagCollection(TagCollection d) => d.innerTagCollection;
        public static explicit operator TagCollection(LibGit2Sharp.TagCollection b) => new TagCollection(b);

        public virtual IEnumerator<Tag> GetEnumerator()
        {
            foreach (var branch in innerTagCollection)
                yield return branch;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Tag this[string name] => innerTagCollection[name];
    }

    public class ReferenceCollection : IEnumerable<Reference>
    {
        private readonly LibGit2Sharp.ReferenceCollection innerReferenceCollection;
        private ReferenceCollection(LibGit2Sharp.ReferenceCollection branchCollection) => innerReferenceCollection = branchCollection;

        protected ReferenceCollection()
        {
        }

        public static implicit operator LibGit2Sharp.ReferenceCollection(ReferenceCollection d) => d.innerReferenceCollection;
        public static explicit operator ReferenceCollection(LibGit2Sharp.ReferenceCollection b) => new ReferenceCollection(b);

        public IEnumerator<Reference> GetEnumerator()
        {
            foreach (var reference in innerReferenceCollection)
                yield return reference;
        }

        public virtual Reference Add(string name, string canonicalRefNameOrObjectish)
        {
            return innerReferenceCollection.Add(name, canonicalRefNameOrObjectish);
        }

        public virtual DirectReference Add(string name, ObjectId targetId)
        {
            return innerReferenceCollection.Add(name, targetId);
        }

        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite)
        {
            return innerReferenceCollection.Add(name, targetId, allowOverwrite);
        }

        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId)
        {
            return innerReferenceCollection.UpdateTarget(directRef, targetId);
        }

        public virtual ReflogCollection Log(string canonicalName)
        {
            return innerReferenceCollection.Log(canonicalName);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Reference this[string name] => innerReferenceCollection[name];
        public virtual Reference Head => this["HEAD"];

        public virtual IEnumerable<Reference> FromGlob(string pattern)
        {
            return innerReferenceCollection.FromGlob(pattern);
        }
    }
}
