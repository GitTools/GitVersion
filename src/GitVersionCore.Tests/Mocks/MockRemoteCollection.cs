using System.Collections;
using System.Collections.Generic;

using LibGit2Sharp;

public class MockRemoteCollection : RemoteCollection, ICollection<Remote>
{
    public List<Remote> Remotes;

    public MockRemoteCollection(IEnumerable<MockRemote> mockRemotes)
    {
        Remotes = new List<Remote>(mockRemotes);
    }

    public override IEnumerator<Remote> GetEnumerator() => Remotes.GetEnumerator();

    IEnumerator<Remote> IEnumerable<Remote>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(Remote item) => Remotes.Add(item);

    public void Clear() => Remotes.Clear();

    public bool Contains(Remote item) => Remotes.Contains(item);

    public void CopyTo(Remote[] array, int arrayIndex) => Remotes.CopyTo(array, arrayIndex);

    public bool Remove(Remote item) => Remotes.Remove(item);

    public int Count => Remotes.Count;

    public bool IsReadOnly => false;
}
