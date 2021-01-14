using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    internal class TagCollection : ITagCollection
    {
        private readonly LibGit2Sharp.TagCollection innerCollection;
        internal TagCollection(LibGit2Sharp.TagCollection collection) => innerCollection = collection;

        public virtual IEnumerator<ITag> GetEnumerator()
        {
            return innerCollection.Select(tag => new Tag(tag)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
