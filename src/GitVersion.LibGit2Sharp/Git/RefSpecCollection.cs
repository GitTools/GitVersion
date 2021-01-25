using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    internal sealed class RefSpecCollection : IRefSpecCollection
    {
        private readonly LibGit2Sharp.RefSpecCollection innerCollection;
        internal RefSpecCollection(LibGit2Sharp.RefSpecCollection collection) => innerCollection = collection;
        public IEnumerator<IRefSpec> GetEnumerator()
        {
            return innerCollection.Select(tag => new RefSpec(tag)).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
