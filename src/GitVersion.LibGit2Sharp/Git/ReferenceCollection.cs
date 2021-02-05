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
            try
            {
                innerCollection.UpdateTarget((Reference)directRef, (ObjectId)targetId);
            }
            catch (LibGit2Sharp.LockedFileException ex)
            {
                // Wrap this exception so that callers that want to catch it don't need to take a dependency on LibGit2Sharp.
                throw new LockedFileException(ex);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IReference? this[string name]
        {
            get
            {
                var reference = innerCollection[name];
                return reference is null ? null : new Reference(reference);
            }
        }

        public IReference? Head => this["HEAD"];

        public IEnumerable<IReference> FromGlob(string pattern)
        {
            return innerCollection.FromGlob(pattern).Select(reference => new Reference(reference));
        }
    }
}
