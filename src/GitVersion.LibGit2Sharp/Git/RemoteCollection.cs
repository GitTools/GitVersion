using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    internal sealed class RemoteCollection : IRemoteCollection
    {
        private readonly LibGit2Sharp.RemoteCollection innerCollection;
        internal RemoteCollection(LibGit2Sharp.RemoteCollection collection) => innerCollection = collection;

        public IEnumerator<IRemote> GetEnumerator()
        {
            return innerCollection.Select(reference => new Remote(reference)).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IRemote? this[string name]
        {
            get
            {
                var remote = innerCollection[name];
                return remote is null ? null : new Remote(remote);
            }
        }

        public void Remove(string remoteName)
        {
            innerCollection.Remove(remoteName);
        }
        public void Update(string remoteName, string refSpec)
        {
            innerCollection.Update(remoteName, r => r.FetchRefSpecs.Add(refSpec));
        }
    }
}
