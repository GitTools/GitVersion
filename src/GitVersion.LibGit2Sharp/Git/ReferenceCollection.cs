using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    public class ReferenceCollection : IReferenceCollection
    {
        private readonly LibGit2Sharp.ReferenceCollection innerCollection;
        internal ReferenceCollection(LibGit2Sharp.ReferenceCollection collection) => innerCollection = collection;

        protected ReferenceCollection()
        {
        }

        public IEnumerator<IReference> GetEnumerator()
        {
            return innerCollection.Select(reference => new Reference(reference)).GetEnumerator();
        }

        public virtual void Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false)
        {
            innerCollection.Add(name, canonicalRefNameOrObjectish, allowOverwrite);
        }

        public virtual void UpdateTarget(IReference directRef, IObjectId targetId)
        {
            innerCollection.UpdateTarget((Reference)directRef, (ObjectId)targetId);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual IReference this[string name] => new Reference(innerCollection[name]);
        public virtual IReference Head => this["HEAD"];

        public virtual IEnumerable<IReference> FromGlob(string pattern)
        {
            return innerCollection.FromGlob(pattern).Select(reference => new Reference(reference));
        }
    }
}
