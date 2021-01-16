using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    internal sealed class ReferenceCollection : IReferenceCollection
    {
        private readonly LibGit2Sharp.ReferenceCollection innerCollection;
        internal ReferenceCollection(LibGit2Sharp.ReferenceCollection collection) => innerCollection = collection;

        public IEnumerator<IReference> GetEnumerator()
        {
            return innerCollection.Select(reference => new Reference(reference)).GetEnumerator();
        }

        public void Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false)
        {
            innerCollection.Add(name, canonicalRefNameOrObjectish, allowOverwrite);
        }

        public void UpdateTarget(IReference directRef, IObjectId targetId)
        {
            innerCollection.UpdateTarget((Reference)directRef, (ObjectId)targetId);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IReference? this[string name]
        {
            get
            {
                var branch = innerCollection[name];
                return branch is null ? null : new Reference(branch);
            }
        }

        public IReference? Head => this["HEAD"];

        public IEnumerable<IReference> FromGlob(string pattern)
        {
            return innerCollection.FromGlob(pattern).Select(reference => new Reference(reference));
        }
    }
}
