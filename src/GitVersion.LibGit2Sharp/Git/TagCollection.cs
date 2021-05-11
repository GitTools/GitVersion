using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    internal sealed class TagCollection : ITagCollection
    {
        private readonly LibGit2Sharp.TagCollection innerCollection;
        internal TagCollection(LibGit2Sharp.TagCollection collection) => innerCollection = collection;

        public IEnumerator<ITag> GetEnumerator()
        {
            return innerCollection.Select(tag => new Tag(tag)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
